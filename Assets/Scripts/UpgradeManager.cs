using System.Collections.Generic;
using TMPro;
using UnityEngine;

public class UpgradeManager : MonoBehaviour
{
    public static UpgradeManager Instance { get; private set; }
    public static int BonusDamage { get; private set; }

    [SerializeField] private GameObject cardPrefab;
    [SerializeField] private GameObject swordPrefab;
    [SerializeField] private int cardsToShow = 3;
    [SerializeField] private int bonusCoinsPerWave = 3;

    public bool IsOpen { get; private set; }

    private enum UpgradeType { Akimbo, Sword, MaxHealth, FireRate, Damage, MoveSpeed, DashCooldown, Snack }

    private class UpgradeInfo
    {
        public string name;
        public string description;
        public int baseCost;
        public int maxBuys;
        public UpgradeType type;
        public Color color;
        public int timesBought;
    }

    private readonly List<UpgradeInfo> upgrades = new List<UpgradeInfo>();
    private readonly List<UpgradeInfo> offered = new List<UpgradeInfo>();
    private readonly List<UpgradeCard> cards = new List<UpgradeCard>();
    private readonly List<GameObject> spawned = new List<GameObject>();
    private readonly List<OrbitSword> swords = new List<OrbitSword>();

    private GameObject player;
    private Health playerHealth;
    private PlayerMovement movement;
    private PlayerDash dash;

    private static readonly Color Gold = new Color(0.95f, 0.78f, 0.30f, 1f);
    private static readonly Color Red = new Color(0.90f, 0.35f, 0.28f, 1f);

    private void Awake()
    {
        Instance = this;
        BonusDamage = 0;

        Color weapon = Red;
        Color health = new Color(0.31f, 0.75f, 0.43f, 1f);
        Color speed = new Color(0.31f, 0.59f, 0.90f, 1f);

        // name, description, cost, max times you can buy it, type, card color
        Add("Akimbo", "A second gun.\nBoth shoot at once.", 12, 1, UpgradeType.Akimbo, weapon);
        Add("Orbit Sword", "A sword spins around\nyou and cuts enemies.", 10, 3, UpgradeType.Sword, Gold);
        Add("Heart Container", "+1 max HP\nand heal 1 HP.", 8, 5, UpgradeType.MaxHealth, health);
        Add("Quick Trigger", "Shoot 20% faster.", 6, 5, UpgradeType.FireRate, weapon);
        Add("Sharp Bullets", "Bullets do +1 damage.", 9, 6, UpgradeType.Damage, weapon);
        Add("Light Boots", "Move 15% faster.", 5, 3, UpgradeType.MoveSpeed, speed);
        Add("Dash Boots", "Dash comes back\n20% sooner.", 5, 3, UpgradeType.DashCooldown, speed);
        Add("Snack", "Heal 2 HP now.", 4, 99, UpgradeType.Snack, health);
    }

    private void OnDestroy()
    {
        if (Instance == this) Instance = null;
    }

    private void Add(string name, string description, int cost, int maxBuys, UpgradeType type, Color color)
    {
        UpgradeInfo info = new UpgradeInfo();
        info.name = name;
        info.description = description;
        info.baseCost = cost;
        info.maxBuys = maxBuys;
        info.type = type;
        info.color = color;
        upgrades.Add(info);
    }

    // ------------------------------------------------------------------ open / close

    public void Open()
    {
        if (IsOpen) return;

        if (cardPrefab == null)
        {
            Debug.LogError("UpgradeManager: drag the UpgradeCard prefab into 'Card Prefab'.");
            return;
        }

        if (!FindPlayer())
        {
            Debug.LogError("UpgradeManager: no object with the 'Player' tag found.");
            return;
        }

        if (bonusCoinsPerWave > 0 && LevelManager.instance != null)
        {
            LevelManager.instance.AddCoin(bonusCoinsPerWave);
            GameFX.Popup(player.transform.position + Vector3.up * 2f, "+" + bonusCoinsPerWave + " coins", Gold, 4f);
        }

        PickCards();
        if (offered.Count == 0)
        {
            Debug.LogWarning("UpgradeManager: no upgrades available to offer.");
            return;
        }

        IsOpen = true;
        SetGuns(false);
        BuildScreen();
    }

    private void Close()
    {
        IsOpen = false;

        for (int i = 0; i < spawned.Count; i++)
        {
            if (spawned[i] != null) Destroy(spawned[i]);
        }

        spawned.Clear();
        cards.Clear();
        offered.Clear();

        // a short delay so the click on the card does not fire a bullet
        Invoke("EnableGuns", 0.2f);
    }

    private void EnableGuns()
    {
        if (playerHealth != null && playerHealth.IsDead) return;
        SetGuns(true);
    }

    private void SetGuns(bool value)
    {
        if (player == null) return;

        Gun[] guns = player.GetComponentsInChildren<Gun>(true);
        for (int i = 0; i < guns.Length; i++)
        {
            guns[i].enabled = value;
        }
    }

    private bool FindPlayer()
    {
        if (player != null) return true;

        player = GameObject.FindGameObjectWithTag("Player");
        if (player == null) return false;

        playerHealth = player.GetComponent<Health>();
        movement = player.GetComponent<PlayerMovement>();
        dash = player.GetComponent<PlayerDash>();
        return true;
    }

    // ------------------------------------------------------------------ choosing and drawing the cards

    private void PickCards()
    {
        offered.Clear();

        List<UpgradeInfo> pool = new List<UpgradeInfo>();
        for (int i = 0; i < upgrades.Count; i++)
        {
            if (IsAvailable(upgrades[i])) pool.Add(upgrades[i]);
        }

        while (offered.Count < cardsToShow && pool.Count > 0)
        {
            int index = Random.Range(0, pool.Count);
            offered.Add(pool[index]);
            pool.RemoveAt(index);
        }
    }

    private bool IsAvailable(UpgradeInfo info)
    {
        if (info.timesBought >= info.maxBuys) return false;

        switch (info.type)
        {
            case UpgradeType.Akimbo:
                return player.GetComponentInChildren<Gun>(true) != null;
            case UpgradeType.Sword:
                return swordPrefab != null;
            case UpgradeType.MaxHealth:
                return playerHealth != null;
            case UpgradeType.MoveSpeed:
                return movement != null;
            case UpgradeType.DashCooldown:
                return dash != null;
            case UpgradeType.Snack:
                return playerHealth != null && playerHealth.CurrentHealth < playerHealth.MaxHealth;
        }

        return true;
    }

    private int GetCost(UpgradeInfo info)
    {
        return info.baseCost + info.timesBought * 2;
    }

    private int GetCoins()
    {
        return LevelManager.instance != null ? LevelManager.instance.coinCount : 0;
    }

    private void BuildScreen()
    {
        Camera cam = Camera.main;
        Vector3 center = cam != null ? cam.transform.position : Vector3.zero;
        center.z = 0f;

        GameObject back = new GameObject("UpgradeBackdrop");
        back.transform.position = center;
        back.transform.localScale = new Vector3(60f, 40f, 1f);
        SpriteRenderer sr = back.AddComponent<SpriteRenderer>();
        sr.sprite = GameFX.Pixel;
        sr.color = new Color(0.05f, 0.04f, 0.10f, 0.7f);
        sr.sortingOrder = 290;
        spawned.Add(back);

        int coins = GetCoins();
        MakeText("CHOOSE AN UPGRADE", center + new Vector3(0f, 3.9f, 0f), 7f, Color.white);
        MakeText("Coins: " + coins + "      [1] [2] [3] or click a card      Enter = skip",
                 center + new Vector3(0f, -3.6f, 0f), 3.2f, Gold);

        float spacing = 3.8f;
        for (int i = 0; i < offered.Count; i++)
        {
            Vector3 position = center + new Vector3((i - (offered.Count - 1) * 0.5f) * spacing, 0f, 0f);
            GameObject go = Instantiate(cardPrefab, position, Quaternion.identity);

            UpgradeCard card = go.GetComponent<UpgradeCard>();
            if (card == null) card = go.AddComponent<UpgradeCard>();

            UpgradeInfo info = offered[i];
            int price = GetCost(info);
            card.Setup(i + 1, info.name, info.description, price, coins >= price, info.color);

            cards.Add(card);
            spawned.Add(go);
        }
    }

    private void MakeText(string text, Vector3 position, float size, Color color)
    {
        GameObject go = new GameObject("UpgradeText");
        go.transform.position = position;

        TextMeshPro tmp = go.AddComponent<TextMeshPro>();
        tmp.text = text;
        tmp.fontSize = size;
        tmp.fontStyle = FontStyles.Bold;
        tmp.alignment = TextAlignmentOptions.Center;
        tmp.color = color;
        tmp.outlineWidth = 0.2f;
        tmp.outlineColor = new Color32(20, 15, 35, 255);
        tmp.rectTransform.sizeDelta = new Vector2(30f, 3f);
        go.GetComponent<MeshRenderer>().sortingOrder = 320;

        spawned.Add(go);
    }

    // ------------------------------------------------------------------ input

    private void Update()
    {
        if (!IsOpen) return;

        if (playerHealth != null && playerHealth.IsDead)
        {
            Close();
            return;
        }

        int choice = -1;

        Camera cam = Camera.main;
        if (cam != null)
        {
            Vector2 mouse = cam.ScreenToWorldPoint(Input.mousePosition);

            for (int i = 0; i < cards.Count; i++)
            {
                if (cards[i] == null) continue;

                bool over = cards[i].Contains(mouse);
                cards[i].SetHover(over);

                if (over && Input.GetMouseButtonDown(0)) choice = i;
            }
        }

        if (Input.GetKeyDown(KeyCode.Alpha1) && cards.Count > 0) choice = 0;
        if (Input.GetKeyDown(KeyCode.Alpha2) && cards.Count > 1) choice = 1;
        if (Input.GetKeyDown(KeyCode.Alpha3) && cards.Count > 2) choice = 2;

        if (choice >= 0)
        {
            TryBuy(choice);
        }
        else if (Input.GetKeyDown(KeyCode.Return) || Input.GetKeyDown(KeyCode.KeypadEnter))
        {
            Close();
        }
    }

    private void TryBuy(int index)
    {
        UpgradeInfo info = offered[index];
        int price = GetCost(info);

        if (GetCoins() < price)
        {
            GameFX.Popup(cards[index].transform.position + Vector3.up * 2.9f, "Not enough coins!", Red, 4f);
            return;
        }

        LevelManager.instance.AddCoin(-price);
        Apply(info);
        info.timesBought++;

        GameFX.Burst(player.transform.position + Vector3.up, new Color[] { Gold, Color.white }, 16, 6f, 0.2f, 0.7f, Vector2.zero);

        Close();
    }

    // ------------------------------------------------------------------ the upgrades

    private void Apply(UpgradeInfo info)
    {
        switch (info.type)
        {
            case UpgradeType.Akimbo: AddAkimbo(); break;
            case UpgradeType.Sword: AddSword(); break;
            case UpgradeType.MaxHealth: playerHealth.AddMaxHealth(1); break;
            case UpgradeType.FireRate: MakeGunsFaster(); break;
            case UpgradeType.Damage: BonusDamage++; break;
            case UpgradeType.MoveSpeed: movement.moveSpeed *= 1.15f; break;
            case UpgradeType.DashCooldown: dash.cooldown = Mathf.Max(0.3f, dash.cooldown * 0.8f); break;
            case UpgradeType.Snack: playerHealth.Heal(2); break;
        }
    }

    // copies your current gun, so the copy has the same bullet and fire point
    private void AddAkimbo()
    {
        Gun first = player.GetComponentInChildren<Gun>(true);
        if (first == null) return;

        GameObject copy = Instantiate(first.gameObject, first.transform.parent);
        copy.name = "Gun (Akimbo)";
        copy.transform.localPosition = first.transform.localPosition + new Vector3(0f, -0.35f, 0f);
    }

    private void AddSword()
    {
        GameObject go = Instantiate(swordPrefab, player.transform.position, Quaternion.identity);

        OrbitSword sword = go.GetComponent<OrbitSword>();
        if (sword == null)
        {
            Destroy(go);
            return;
        }

        swords.Add(sword);

        // spread the swords evenly around the player
        for (int i = 0; i < swords.Count; i++)
        {
            if (swords[i] != null)
            {
                swords[i].SetAngle(360f * i / swords.Count);
            }
        }
    }

    private void MakeGunsFaster()
    {
        Gun[] guns = player.GetComponentsInChildren<Gun>(true);
        for (int i = 0; i < guns.Length; i++)
        {
            guns[i].fireRate = Mathf.Max(0.05f, guns[i].fireRate * 0.8f);
        }
    }
}