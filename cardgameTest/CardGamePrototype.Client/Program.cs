using System.Numerics;
using System.Text.Json;
using Raylib_cs;
using CardGamePrototype.Core;

namespace CardGamePrototype.Client;

public enum GameScene { TitleScreen, ModeSelect, CampaignMap, BattleView, MarketShop, DeckBuilder, RewardChoice, CampaignVictory }
public enum GameTheme { SciFi, Fantasy }
public enum NodeType { CombatMinion, CombatElite, CombatBoss }

public class CampaignNode
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public NodeType Type { get; set; }
    public int EnemyHp { get; set; }
    public int EnemyDefaultPosition { get; set; }
    public bool Completed { get; set; }
    public float MapX { get; set; } = 0.5f;
    public float MapY { get; set; } = 0.5f;
    public List<int> ChildIds { get; set; } = new();
}

// Floating damage number particle
public struct FloatDmg
{
    public float X, Y, Vy;
    public int Amount;
    public float Life;  // 1.0 → 0 over ~1.2 s
    public Color Col;
}

public class PlayerProfile
{
    public int Gold { get; set; } = 150;
    public int SkillPoints { get; set; } = 8;
    public int PlayerHp { get; set; } = 0; // 0 = use max (full heal)
    public List<CardDefinition> TotalCollection { get; set; } = new();
    public List<CardDefinition> ActiveDeck { get; set; } = new();
    public SkillTreeManager SkillTree { get; set; } = new();
    public AbilityDefinition? SelectedAbility { get; set; }

    bool HasNode(int id) => SkillTree.Nodes.Find(n => n.Id == id)?.IsUnlocked == true;

    public int MaxEnergyBonus  => (HasNode(1) ? 1 : 0) + (HasNode(2) ? 1 : 0) + (HasNode(3) ? 2 : 0);
    public int MaxHpBonus      => (HasNode(11) ? 10 : 0) + (HasNode(12) ? 15 : 0) + (HasNode(13) ? 25 : 0);
    public int StartCardsBonus => (HasNode(4) ? 1 : 0) + (HasNode(5) ? 1 : 0);
}

class SaveData
{
    public int Gold { get; set; }
    public int SkillPoints { get; set; }
    public int PlayerHp { get; set; }
    public List<string> CollectionIds  { get; set; } = new();
    public List<string> DeckIds        { get; set; } = new();
    public string? AbilityId           { get; set; }
    public List<int> UnlockedSkillIds  { get; set; } = new();
    public List<int> CompletedNodeIds  { get; set; } = new();
}

class Program
{
    static string SavePath() => Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
        "CatalystArchitecture", "save.json");

    static void SaveGame(PlayerProfile profile, HashSet<int> completedNodes)
    {
        var data = new SaveData
        {
            Gold          = profile.Gold,
            SkillPoints   = profile.SkillPoints,
            PlayerHp      = profile.PlayerHp,
            CollectionIds = profile.TotalCollection.Select(c => c.Id).ToList(),
            DeckIds       = profile.ActiveDeck.Select(c => c.Id).ToList(),
            AbilityId     = profile.SelectedAbility?.Id,
            UnlockedSkillIds = profile.SkillTree.Nodes.Where(n => n.IsUnlocked).Select(n => n.Id).ToList(),
            CompletedNodeIds = completedNodes.ToList(),
        };
        string dir = Path.GetDirectoryName(SavePath())!;
        if (!Directory.Exists(dir)) Directory.CreateDirectory(dir);
        File.WriteAllText(SavePath(), JsonSerializer.Serialize(data, new JsonSerializerOptions { WriteIndented = true }));
    }

    static bool LoadGame(PlayerProfile profile, HashSet<int> completedNodes, List<CampaignNode> campaignNodes)
    {
        string path = SavePath();
        if (!File.Exists(path)) return false;
        try
        {
            var data = JsonSerializer.Deserialize<SaveData>(File.ReadAllText(path));
            if (data == null) return false;
            profile.Gold        = data.Gold;
            profile.SkillPoints = data.SkillPoints;
            profile.PlayerHp    = data.PlayerHp;

            var allCards = CardLibrary.GetAll().ToDictionary(c => c.Id);
            profile.TotalCollection.Clear();
            foreach (var id in data.CollectionIds) if (allCards.TryGetValue(id, out var c)) profile.TotalCollection.Add(c);
            profile.ActiveDeck.Clear();
            foreach (var id in data.DeckIds) if (allCards.TryGetValue(id, out var c)) profile.ActiveDeck.Add(c);

            var allAbils = AbilityLibrary.GetAll().ToDictionary(a => a.Id);
            if (data.AbilityId != null && allAbils.TryGetValue(data.AbilityId, out var ab))
                profile.SelectedAbility = ab;

            foreach (var id in data.UnlockedSkillIds)
            {
                var node = profile.SkillTree.Nodes.Find(n => n.Id == id);
                if (node != null) node.IsUnlocked = true;
            }
            completedNodes.Clear();
            foreach (var id in data.CompletedNodeIds)
            {
                completedNodes.Add(id);
                var cn = campaignNodes.Find(n => n.Id == id);
                if (cn != null) cn.Completed = true;
            }
            return true;
        }
        catch { return false; }
    }

    static bool DrawButton(Rectangle rect, string text, Color baseColor, Color hoverColor)
    {
        Vector2 mouse = Raylib.GetMousePosition();
        bool hovered = Raylib.CheckCollisionPointRec(mouse, rect);
        Raylib.DrawRectangleRounded(rect, 0.14f, 6, hovered ? hoverColor : baseColor);
        // Top-edge highlight gives slight 3-D lift
        Raylib.DrawLineEx(new Vector2(rect.X + 6, rect.Y + 1.5f), new Vector2(rect.X + rect.Width - 6, rect.Y + 1.5f), 1f, new Color(255, 255, 255, 38));
        Raylib.DrawRectangleRoundedLinesEx(rect, 0.14f, 6, 1.5f, hovered ? new Color(185, 190, 210, 160) : new Color(70, 75, 95, 130));

        int textWidth = Raylib.MeasureText(text, 16);
        Raylib.DrawText(text, (int)(rect.X + rect.Width / 2 - textWidth / 2), (int)(rect.Y + rect.Height / 2 - 8), 16, new Color(225, 228, 238, 255));

        return hovered && Raylib.IsMouseButtonPressed(MouseButton.Left);
    }

    static void DrawCard(Rectangle rect, string title, string description, int cost, Color body, Color border, bool selected = false)
    {
        Raylib.DrawRectangleRounded(rect, 0.08f, 8, body);
        Raylib.DrawRectangleRoundedLinesEx(rect, 0.08f, 8, selected ? 3f : 1.5f, selected ? Color.Gold : border);
        if (selected)
            Raylib.DrawRectangleRoundedLinesEx(new Rectangle(rect.X - 3, rect.Y - 3, rect.Width + 6, rect.Height + 6), 0.1f, 8, 1f, new Color(255, 210, 0, 80));

        // Header overlay — semi-transparent so it works on any body color
        Raylib.DrawRectangle((int)rect.X + 1, (int)rect.Y + 1, (int)rect.Width - 2, 29, new Color(0, 0, 0, 140));
        Raylib.DrawText(title, (int)rect.X + 8, (int)rect.Y + 8, 13, Color.White);
        Raylib.DrawLineEx(new Vector2(rect.X + 6, rect.Y + 30f), new Vector2(rect.X + rect.Width - 26, rect.Y + 30f), 1f, new Color(255, 255, 255, 22));

        // Description
        Raylib.DrawText(description, (int)rect.X + 8, (int)rect.Y + 36, 11, new Color(155, 162, 180, 255));

        // Cost badge — number centered, no icon clutter
        Vector2 badge = new(rect.X + rect.Width - 15, rect.Y + 15);
        Raylib.DrawCircleV(badge, 12, new Color(0, 72, 162, 255));
        Raylib.DrawCircleLines((int)badge.X, (int)badge.Y, 12, new Color(68, 132, 238, 200));
        int costW = Raylib.MeasureText(cost.ToString(), 13);
        Raylib.DrawText(cost.ToString(), (int)(badge.X - costW / 2), (int)(badge.Y - 7), 13, Color.White);
    }

    static Color CardColorFromName(string name) => name switch
    {
        "Ignite"                    => new Color(235, 95,  15,  255),
        "Firebolt"                  => new Color(255, 145, 35,  255),
        "Inferno"                   => new Color(210, 55,  20,  255),
        "Phoenix Ash" or "PhoenixAsh" => new Color(255, 170, 50, 255),
        "Storm Strike" or "StormStrike" => new Color(235, 95, 15, 255),
        "Frost Nova" or "FrostNova" => new Color(65,  175, 240, 255),
        "Cryo Shell" or "CryoShell" => new Color(95,  210, 255, 255),
        "Push"                      => new Color(155, 75,  215, 255),
        "Bio Spore" or "BioSpore"   => new Color(45,  200, 65,  255),
        "Spore Cloud" or "SporeCloud" => new Color(80, 210, 100, 255),
        "Iron Guard" or "IronGuard" => new Color(155, 160, 175, 255),
        "Slag Golem" or "SlagGolem" => new Color(120, 115, 105, 255),
        "Volt Strike" or "VoltStrike" => new Color(245, 220, 30, 255),
        _                           => new Color(140, 145, 165, 255)
    };

    static void DrawSummonedUnit(Rectangle zone, string cardName, int hp, int maxHp, bool selected)
    {
        Color col = CardColorFromName(cardName);
        Raylib.DrawRectangleRounded(zone, 0.12f, 6, new Color(col.R / 5, col.G / 5, col.B / 5, 255));
        Raylib.DrawRectangleRoundedLinesEx(zone, 0.12f, 6, selected ? 3 : 2, selected ? Color.Gold : col);

        float cx = zone.X + zone.Width / 2f;
        float cy = zone.Y + zone.Height * 0.42f;
        float r = Math.Min(zone.Width, zone.Height) * 0.27f;

        if (cardName is "Ignite" or "Firebolt")
        {
            // Flame: two stacked upward triangles
            Raylib.DrawTriangle(new Vector2(cx, cy - r), new Vector2(cx - r * 0.55f, cy + r * 0.5f), new Vector2(cx + r * 0.55f, cy + r * 0.5f), col);
            Raylib.DrawTriangle(new Vector2(cx, cy - r * 0.3f), new Vector2(cx - r * 0.28f, cy + r * 0.9f), new Vector2(cx + r * 0.28f, cy + r * 0.9f), new Color(255, 215, 60, 210));
        }
        else if (cardName is "Frost Nova" or "FrostNova")
        {
            // Snowflake: 4-axis cross
            Raylib.DrawLineEx(new Vector2(cx - r, cy), new Vector2(cx + r, cy), 3f, col);
            Raylib.DrawLineEx(new Vector2(cx, cy - r), new Vector2(cx, cy + r), 3f, col);
            Raylib.DrawLineEx(new Vector2(cx - r * 0.72f, cy - r * 0.72f), new Vector2(cx + r * 0.72f, cy + r * 0.72f), 3f, col);
            Raylib.DrawLineEx(new Vector2(cx + r * 0.72f, cy - r * 0.72f), new Vector2(cx - r * 0.72f, cy + r * 0.72f), 3f, col);
        }
        else if (cardName is "Push")
        {
            // Arrow: right-facing chevron + tail
            Raylib.DrawTriangle(new Vector2(cx + r, cy), new Vector2(cx - r * 0.3f, cy - r * 0.7f), new Vector2(cx - r * 0.3f, cy + r * 0.7f), col);
            Raylib.DrawRectangleRec(new Rectangle(cx - r, cy - r * 0.22f, r * 0.78f, r * 0.44f), col);
        }
        else if (cardName is "Bio Spore" or "BioSpore")
        {
            // Bio: concentric rings + nucleus
            Raylib.DrawCircleLines((int)cx, (int)cy, r, col);
            Raylib.DrawCircleLines((int)cx, (int)cy, r * 0.58f, col);
            Raylib.DrawCircleV(new Vector2(cx, cy), r * 0.24f, col);
        }
        else if (cardName is "Iron Guard" or "IronGuard" or "Slag Golem" or "SlagGolem" or "Cryo Shell" or "CryoShell")
        {
            // Shield shape: rect + triangle top
            Raylib.DrawRectangle((int)(cx - r * 0.55f), (int)(cy - r * 0.1f), (int)(r * 1.1f), (int)(r * 0.9f), col);
            Raylib.DrawTriangle(new Vector2(cx, cy - r * 0.9f), new Vector2(cx - r * 0.55f, cy - r * 0.1f), new Vector2(cx + r * 0.55f, cy - r * 0.1f), col);
        }
        else if (cardName is "Volt Strike" or "VoltStrike")
        {
            // Lightning bolt
            Raylib.DrawTriangle(new Vector2(cx + r * 0.2f, cy - r), new Vector2(cx - r * 0.5f, cy + r * 0.1f), new Vector2(cx + r * 0.1f, cy + r * 0.1f), col);
            Raylib.DrawTriangle(new Vector2(cx - r * 0.1f, cy - r * 0.1f), new Vector2(cx - r * 0.2f, cy + r), new Vector2(cx + r * 0.5f, cy - r * 0.1f), col);
        }
        else if (cardName is "Inferno" or "Phoenix Ash" or "PhoenixAsh" or "Storm Strike" or "StormStrike")
        {
            // Flame (same as Ignite/Firebolt)
            Raylib.DrawTriangle(new Vector2(cx, cy - r), new Vector2(cx - r * 0.55f, cy + r * 0.5f), new Vector2(cx + r * 0.55f, cy + r * 0.5f), col);
            Raylib.DrawTriangle(new Vector2(cx, cy - r * 0.3f), new Vector2(cx - r * 0.28f, cy + r * 0.9f), new Vector2(cx + r * 0.28f, cy + r * 0.9f), new Color(255, 215, 60, 210));
        }
        else if (cardName is "Spore Cloud" or "SporeCloud")
        {
            // Bio rings (same as BioSpore)
            Raylib.DrawCircleLines((int)cx, (int)cy, r, col);
            Raylib.DrawCircleLines((int)cx, (int)cy, r * 0.58f, col);
            Raylib.DrawCircleV(new Vector2(cx, cy), r * 0.24f, col);
        }
        else
        {
            // Generic: diamond
            Raylib.DrawTriangle(new Vector2(cx, cy - r), new Vector2(cx - r * 0.55f, cy), new Vector2(cx + r * 0.55f, cy), col);
            Raylib.DrawTriangle(new Vector2(cx, cy + r), new Vector2(cx + r * 0.55f, cy), new Vector2(cx - r * 0.55f, cy), col);
        }

        int nw = Raylib.MeasureText(cardName, 11);
        Raylib.DrawText(cardName, (int)(zone.X + (zone.Width - nw) / 2), (int)(zone.Y + zone.Height - 30), 11, Color.White);

        float bx = zone.X + 5, by = zone.Y + zone.Height - 16, bw = zone.Width - 10, bh = 7;
        Raylib.DrawRectangleRec(new Rectangle(bx, by, bw, bh), new Color(38, 38, 38, 220));
        if (maxHp > 0)
            Raylib.DrawRectangleRec(new Rectangle(bx, by, bw * ((float)hp / maxHp), bh), new Color(50, 215, 80, 255));
    }

    // Radial skill tree layout — 5 branches fan out 72° apart from the origin node
    static Vector2 GetSkillNodePos(SkillNode node, float originX, float originY, float spacing = 78f)
    {
        if (node.Branch == SkillBranch.Origin) return new Vector2(originX, originY);
        float deg = node.Branch switch
        {
            SkillBranch.Green  => 270f,
            SkillBranch.Blue   => 198f,
            SkillBranch.Red    => 342f,
            SkillBranch.Yellow => 126f,
            SkillBranch.White  =>  54f,
            _ => 0f
        };
        float rad = deg * MathF.PI / 180f;
        return new Vector2(originX + node.Column * spacing * MathF.Cos(rad),
                           originY + node.Column * spacing * MathF.Sin(rad));
    }

    static void DrawNodeIcon(Vector2 c, SkillBranch branch, Color col, float r = 9f)
    {
        switch (branch)
        {
            case SkillBranch.Origin:
                Raylib.DrawCircleV(c, r * 0.5f, col); break;
            case SkillBranch.Green:
                Raylib.DrawLineEx(new Vector2(c.X + r * 0.25f, c.Y - r), new Vector2(c.X - r * 0.15f, c.Y + r * 0.05f), 2f, col);
                Raylib.DrawLineEx(new Vector2(c.X - r * 0.15f, c.Y + r * 0.05f), new Vector2(c.X + r * 0.3f, c.Y + r * 0.2f), 2f, col);
                Raylib.DrawLineEx(new Vector2(c.X + r * 0.3f, c.Y + r * 0.2f), new Vector2(c.X - r * 0.25f, c.Y + r), 2f, col);
                break;
            case SkillBranch.Blue:
                Raylib.DrawCircleLines((int)c.X, (int)c.Y, r * 0.72f, col);
                Raylib.DrawCircleV(c, r * 0.3f, col); break;
            case SkillBranch.Red:
                Raylib.DrawTriangle(new Vector2(c.X, c.Y - r), new Vector2(c.X - r * 0.55f, c.Y + r * 0.65f), new Vector2(c.X + r * 0.55f, c.Y + r * 0.65f), col); break;
            case SkillBranch.Yellow:
                Raylib.DrawCircleLines((int)c.X, (int)c.Y, r * 0.68f, col);
                Raylib.DrawLineEx(new Vector2(c.X, c.Y - r * 0.38f), new Vector2(c.X, c.Y + r * 0.38f), 1.5f, col); break;
            case SkillBranch.White:
                Raylib.DrawLineEx(new Vector2(c.X - r, c.Y), new Vector2(c.X + r, c.Y), 2.5f, col);
                Raylib.DrawLineEx(new Vector2(c.X, c.Y - r), new Vector2(c.X, c.Y + r), 2.5f, col); break;
        }
    }

    static void DrawBackground(int width, int height, GameTheme t = GameTheme.SciFi)
    {
        if (t == GameTheme.SciFi)
        {
            // 8-bit pixel grid on pure black
            for (int x = 0; x < width; x += 16)
                Raylib.DrawLine(x, 0, x, height, new Color(0, 38, 0, 60));
            for (int y = 0; y < height; y += 16)
                Raylib.DrawLine(0, y, width, y, new Color(0, 38, 0, 60));
            // Scanline overlay
            for (int y = 0; y < height; y += 3)
                Raylib.DrawRectangle(0, y, width, 1, new Color(0, 0, 0, 55));
        }
        else
        {
            // Fantasy: deep stone dungeon base
            Raylib.ClearBackground(new Color(8, 5, 3, 255));
            // Heavy stone block pattern
            for (int x = 0; x < width; x += 80)  Raylib.DrawLine(x, 0, x, height, new Color(18, 12, 6, 255));
            for (int y = 0; y < height; y += 60)  Raylib.DrawLine(0, y, width, y, new Color(15, 10, 5, 255));
            // Mortar offset rows (staggered brick feel)
            for (int y = 30; y < height; y += 60) Raylib.DrawLine(0, y, width, y, new Color(12, 8, 4, 220));
            // Top vignette (oppressive ceiling)
            Raylib.DrawRectangleGradientV(0, 0, width, 80, new Color(0, 0, 0, 200), new Color(0, 0, 0, 0));
            // Bottom vignette
            Raylib.DrawRectangleGradientV(0, height - 60, width, 60, new Color(0, 0, 0, 0), new Color(0, 0, 0, 160));
        }
    }

    // Returns true if the ACTIVATE button was clicked
    static bool DrawAbilityBar(float x, float y, float w, float h, BattleState bs, bool playerTurn)
    {
        if (bs.EquippedAbility == null) return false;
        var ab = bs.EquippedAbility;
        float frac = Math.Clamp(bs.AbilityCharge / ab.MaxCharge, 0f, 1f);
        bool ready = frac >= 1f;

        Raylib.DrawRectangleRec(new Rectangle(x, y, w, h), new Color(14, 16, 24, 220));
        Raylib.DrawRectangleLinesEx(new Rectangle(x, y, w, h), 1f, ready ? Color.Gold : new Color(55, 58, 74, 200));

        float barW = w - 120;
        DrawBar(x + 4, y + h / 2 - 4, barW, 8, frac, ready ? Color.Gold : new Color(68, 148, 222, 255), new Color(22, 26, 36, 255));

        int nameW = Raylib.MeasureText(ab.Name, 12);
        Raylib.DrawText(ab.Name, (int)(x + 4), (int)(y + 4), 12, ready ? Color.Gold : new Color(130, 135, 158, 255));

        int pctW = Raylib.MeasureText($"{(int)(frac * 100)}%", 11);
        Raylib.DrawText($"{(int)(frac * 100)}%", (int)(x + barW - pctW + 4), (int)(y + h / 2 - 7), 11, new Color(115, 120, 140, 255));

        Rectangle btn = new(x + w - 112, y + 3, 108, h - 6);
        bool canAct = ready && playerTurn;
        bool clicked = DrawButton(btn, "ACTIVATE", canAct ? new Color(80, 55, 18, 255) : new Color(25, 26, 34, 255),
                                                   canAct ? new Color(130, 95, 28, 255) : new Color(30, 32, 42, 255));
        if (!canAct)
        {
            // grey out text
            int txtW = Raylib.MeasureText("ACTIVATE", 16);
            Raylib.DrawRectangleRec(btn, new Color(0, 0, 0, 120));
        }
        return clicked && canAct;
    }

    static void DrawBar(float x, float y, float w, float h, float frac, Color fill, Color bg)
    {
        Raylib.DrawRectangleRec(new Rectangle(x, y, w, h), bg);
        Raylib.DrawRectangleRec(new Rectangle(x, y, w * Math.Clamp(frac, 0f, 1f), h), fill);
    }

    static Color GetBranchColor(SkillBranch branch)
    {
        return branch switch
        {
            SkillBranch.Green => new Color(50, 200, 90, 255),
            SkillBranch.Blue => Color.SkyBlue,
            SkillBranch.Red => Color.Maroon,
            SkillBranch.Yellow => Color.Gold,
            SkillBranch.White => Color.LightGray,
            _ => Color.Purple
        };
    }

    static void Main()
    {
        GameScene scene = GameScene.TitleScreen;
        GameTheme theme = GameTheme.SciFi;
        PlayerProfile profile = new PlayerProfile();

        // Starter collection — 6 unique cards player begins with
        var starterSet = new[]
        {
            CardLibrary.Ignite(), CardLibrary.Firebolt(), CardLibrary.Push(),
            CardLibrary.FrostNova(), CardLibrary.BioSpore(), CardLibrary.IronGuard()
        };
        foreach (var c in starterSet) profile.TotalCollection.Add(c);
        profile.ActiveDeck.AddRange(starterSet);
        profile.SelectedAbility = AbilityLibrary.Overclock();

        // Branching campaign map
        List<CampaignNode> campaignNodes = new List<CampaignNode>
        {
            new CampaignNode { Id=0, Name="Catalyst Entrance",        Type=NodeType.CombatMinion, EnemyHp=30,  EnemyDefaultPosition=2, MapX=0.08f, MapY=0.50f, ChildIds=new(){1,2,3} },
            new CampaignNode { Id=1, Name="Upper Flank",               Type=NodeType.CombatMinion, EnemyHp=44,  EnemyDefaultPosition=0, MapX=0.37f, MapY=0.18f, ChildIds=new(){4} },
            new CampaignNode { Id=2, Name="Core Passage",              Type=NodeType.CombatMinion, EnemyHp=50,  EnemyDefaultPosition=2, MapX=0.37f, MapY=0.50f, ChildIds=new(){4} },
            new CampaignNode { Id=3, Name="Lower Grid",                Type=NodeType.CombatMinion, EnemyHp=44,  EnemyDefaultPosition=4, MapX=0.37f, MapY=0.82f, ChildIds=new(){4} },
            new CampaignNode { Id=4, Name="Arch-Executioner Frame",    Type=NodeType.CombatElite,  EnemyHp=75,  EnemyDefaultPosition=3, MapX=0.65f, MapY=0.50f, ChildIds=new(){5} },
            new CampaignNode { Id=5, Name="The Catalyst Singularity",  Type=NodeType.CombatBoss,   EnemyHp=120, EnemyDefaultPosition=2, MapX=0.90f, MapY=0.50f, ChildIds=new() },
        };
        HashSet<int> completedNodes = new();

        BattleService battleService = new BattleService();
        CampaignNode? activeBattleNode = null;

        // Post-combat reward
        List<CardDefinition> rewardOptions = new();
        Random rewardRng = new();

        List<CardDefinition> shopInventory = new List<CardDefinition>
        {
            CardLibrary.Ignite(), CardLibrary.Firebolt(), CardLibrary.CryoShell(),
            CardLibrary.SporeCloud(), CardLibrary.PhoenixAsh(), CardLibrary.SlagGolem(),
            CardLibrary.VoltStrike(), CardLibrary.Inferno()
        };
        int cardShopCost = 45;

        Raylib.InitWindow(1600, 900, "Catalyst Architecture");
        Raylib.SetWindowState(ConfigFlags.ResizableWindow);
        Raylib.SetTargetFPS(60);
        Raylib.InitAudioDevice();

        string srcDir = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Sources");
        Sound[] whooshSounds = new Sound[5];
        for (int wi = 0; wi < 5; wi++)
            whooshSounds[wi] = Raylib.LoadSound(Path.Combine(srcDir, $"Whoosh_{wi + 1}.mp3"));
        Sound[] hitSounds = new Sound[4];
        for (int hi = 0; hi < 4; hi++)
            hitSounds[hi] = Raylib.LoadSound(Path.Combine(srcDir, $"hit_{hi + 1}.mp3"));
        Sound whoosh6   = Raylib.LoadSound(Path.Combine(srcDir, "Whoosh_6.mp3")); // ability charged
        Sound whoosh7   = Raylib.LoadSound(Path.Combine(srcDir, "Whoosh_7.mp3")); // ability use
        Sound winSound  = Raylib.LoadSound(Path.Combine(srcDir, "win_1.mp3"));
        Sound loseSound = Raylib.LoadSound(Path.Combine(srcDir, "lose_1.mp3"));
        Sound ui1Sound  = Raylib.LoadSound(Path.Combine(srcDir, "ui_1.mp3"));
        Sound ui2Sound  = Raylib.LoadSound(Path.Combine(srcDir, "ui_2.mp3"));
        Random sfxRng = new Random();
        bool battleEndSoundPlayed = false;
        float prevAbilityCharge   = -1f;

        bool dragging = false;
        int draggingIndex = -1;
        Vector2 dragOffset = Vector2.Zero;
        bool isDraggingFromCollectionPool = false;
        List<FloatDmg> floatDmgs = new();
        float enemyFlashTimer = 0f;
        Vector2 enemyScreenPos = Vector2.Zero;

        // Camera shake
        float shakeTimer = 0f;
        Vector2 shakeOffset = Vector2.Zero;

        // Unit death flash (one per board slot)
        float[] slotFlashTimers = new float[5];
        bool[] prevSlotOccupied = new bool[5];

        // Turn banner
        string turnBannerText = "";
        float turnBannerTimer = 0f;

        // Pause menu
        bool isPaused = false;
        bool showBurnPile = false;
        bool confirmRestart = false;
        float masterVolume = 1.0f;

        // Animation state
        float displayedEnemyHp  = -1f;
        float displayedPlayerHp = -1f;
        float enemyBobTime = 0f;
        Raylib.SetMasterVolume(masterVolume);

        // Load saved run if one exists
        LoadGame(profile, completedNodes, campaignNodes);

        bool exitGame = false;
        while (!Raylib.WindowShouldClose() && !exitGame)
        {
            int width = Raylib.GetScreenWidth();
            int height = Raylib.GetScreenHeight();
            Vector2 mouse = Raylib.GetMousePosition();
            float dt = Raylib.GetFrameTime();

            // GLOBAL: Listen for F3 to toggle Skill Tree debug
            if (Raylib.IsKeyPressed(KeyboardKey.F3))
                profile.SkillTree.DebugMode = !profile.SkillTree.DebugMode;
            if (Raylib.IsKeyPressed(KeyboardKey.F5))
                theme = theme == GameTheme.SciFi ? GameTheme.Fantasy : GameTheme.SciFi;

            // P: toggle pause in battle
            if (Raylib.IsKeyPressed(KeyboardKey.P) && scene == GameScene.BattleView)
            {
                if (showBurnPile) showBurnPile = false;
                else isPaused = !isPaused;
            }

            // ESC: navigate back / close game
            if (Raylib.IsKeyPressed(KeyboardKey.Escape))
            {
                if (scene == GameScene.TitleScreen)
                    exitGame = true;
                else if (scene == GameScene.ModeSelect)
                    scene = GameScene.TitleScreen;
                else if (scene == GameScene.DeckBuilder || scene == GameScene.MarketShop)
                    scene = GameScene.CampaignMap;
                else if (scene == GameScene.BattleView)
                {
                    if (showBurnPile) showBurnPile = false;
                }
            }

            // Animation: HP bar lerp + enemy bob
            if (scene == GameScene.BattleView)
            {
                var bsA = battleService.State;
                enemyBobTime += dt;
                if (displayedEnemyHp < 0)  { displayedEnemyHp  = bsA.Enemy.Hp;  displayedPlayerHp = bsA.Player.Hp; }
                float lf = Math.Min(1f, dt * 14f);
                displayedEnemyHp  += (bsA.Enemy.Hp  - displayedEnemyHp)  * lf;
                displayedPlayerHp += (bsA.Player.Hp - displayedPlayerHp) * lf;
            }

            // Shake decay
            if (shakeTimer > 0)
            {
                shakeTimer = Math.Max(0f, shakeTimer - dt);
                float intensity = shakeTimer / 0.25f;
                shakeOffset = scene == GameScene.BattleView
                    ? new Vector2((float)(sfxRng.NextDouble() * 2 - 1) * 8f * intensity,
                                  (float)(sfxRng.NextDouble() * 2 - 1) * 8f * intensity)
                    : Vector2.Zero;
            }
            else shakeOffset = Vector2.Zero;

            // Turn banner decay
            if (turnBannerTimer > 0) turnBannerTimer -= dt;

            // Slot flash decay
            for (int fi = 0; fi < 5; fi++) if (slotFlashTimers[fi] > 0) slotFlashTimers[fi] -= dt;

            Raylib.BeginDrawing();
            if (theme == GameTheme.SciFi)
                Raylib.ClearBackground(new Color(0, 0, 0, 255));
            else
                Raylib.ClearBackground(new Color(8, 5, 3, 255));
            DrawBackground(width, height, theme);

            // Camera shake via Mode2D offset
            var shakeCam = new Camera2D { Target = Vector2.Zero, Offset = shakeOffset, Rotation = 0f, Zoom = 1.0f };
            Raylib.BeginMode2D(shakeCam);

            switch (scene)
            {
                case GameScene.TitleScreen:
                    // Title block
                    Raylib.DrawRectangle(width / 2 - 370, height / 2 - 190, 740, 230, new Color(16, 20, 30, 200));
                    Raylib.DrawLineEx(new Vector2(width / 2 - 340f, height / 2 - 108f), new Vector2(width / 2 + 340f, height / 2 - 108f), 1f, new Color(100, 85, 30, 180));

                    int t1w = Raylib.MeasureText("CATALYST", 76);
                    Raylib.DrawText("CATALYST", width / 2 - t1w / 2, height / 2 - 188, 76, Color.Gold);
                    int t2w = Raylib.MeasureText("//  ARCHITECTURE", 34);
                    Raylib.DrawText("//  ARCHITECTURE", width / 2 - t2w / 2, height / 2 - 108, 34, new Color(198, 168, 60, 255));
                    Raylib.DrawLineEx(new Vector2(width / 2 - 340f, height / 2 - 66f), new Vector2(width / 2 + 340f, height / 2 - 66f), 1f, new Color(100, 85, 30, 180));

                    int subW = Raylib.MeasureText("Deterministic Tactical Command Interface", 15);
                    Raylib.DrawText("Deterministic Tactical Command Interface", width / 2 - subW / 2, height / 2 - 46, 15, new Color(130, 136, 158, 255));

                    if (DrawButton(new Rectangle(width / 2 - 145, height / 2 + 22, 290, 48), "INITIALIZE CORE SYSTEM", new Color(38, 50, 72, 255), new Color(58, 80, 122, 255)))
                        scene = GameScene.ModeSelect;

                    int vw = Raylib.MeasureText("ALPHA  v0.1", 12);
                    Raylib.DrawText("ALPHA  v0.1", width - vw - 14, height - 22, 12, new Color(55, 60, 78, 255));
                    break;

                case GameScene.ModeSelect:
                    int msHdr = Raylib.MeasureText("SELECT SYSTEM PROTOCOL", 30);
                    Raylib.DrawText("SELECT SYSTEM PROTOCOL", width / 2 - msHdr / 2, 140, 30, Color.White);
                    Raylib.DrawLineEx(new Vector2(width / 2 - 220f, 178f), new Vector2(width / 2 + 220f, 178f), 1f, new Color(62, 65, 82, 255));

                    // Story mode — clickable
                    Rectangle storyR = new(width / 2 - 320, height / 2 - 70, 280, 130);
                    bool storyHov = Raylib.CheckCollisionPointRec(mouse, storyR);
                    Raylib.DrawRectangleRounded(storyR, 0.1f, 6, storyHov ? new Color(45, 115, 75, 255) : new Color(28, 75, 48, 255));
                    Raylib.DrawLineEx(new Vector2(storyR.X + 6, storyR.Y + 1.5f), new Vector2(storyR.X + storyR.Width - 6, storyR.Y + 1.5f), 1f, new Color(255, 255, 255, 38));
                    Raylib.DrawRectangleRoundedLinesEx(storyR, 0.1f, 6, 1.5f, new Color(58, 168, 95, 160));
                    Raylib.DrawText("STORY MODE", (int)storyR.X + 48, (int)storyR.Y + 30, 22, Color.White);
                    Raylib.DrawText("Sector Campaign", (int)storyR.X + 60, (int)storyR.Y + 60, 14, new Color(135, 210, 155, 255));
                    Raylib.DrawText("Full roguelike run", (int)storyR.X + 60, (int)storyR.Y + 80, 12, new Color(85, 148, 100, 255));
                    if (storyHov && Raylib.IsMouseButtonPressed(MouseButton.Left)) scene = GameScene.CampaignMap;

                    // Arcade mode — locked
                    Rectangle arcR = new(width / 2 + 40, height / 2 - 70, 280, 130);
                    Raylib.DrawRectangleRounded(arcR, 0.1f, 6, new Color(24, 24, 30, 255));
                    Raylib.DrawRectangleRoundedLinesEx(arcR, 0.1f, 6, 1.5f, new Color(46, 48, 60, 255));
                    Raylib.DrawText("ARCADE MODE", (int)arcR.X + 48, (int)arcR.Y + 30, 22, new Color(72, 74, 90, 255));
                    Raylib.DrawText("Coming soon", (int)arcR.X + 78, (int)arcR.Y + 60, 14, new Color(85, 48, 48, 255));
                    Raylib.DrawText("LOCKED IN ALPHA", (int)arcR.X + 54, (int)arcR.Y + 80, 12, new Color(110, 50, 50, 255));
                    break;

                case GameScene.CampaignMap:
                    Raylib.DrawRectangle(0, 0, width, 58, new Color(14, 16, 21, 255));
                    Raylib.DrawLine(0, 58, width, 58, new Color(35, 40, 52, 255));
                    Raylib.DrawText("CAMPAIGN", 28, 16, 22, Color.White);
                    int mapMaxHp  = 50 + profile.MaxHpBonus;
                    int mapHpDisp = profile.PlayerHp > 0 ? profile.PlayerHp : mapMaxHp;
                    Raylib.DrawText($"HP {mapHpDisp}/{mapMaxHp}", width - 420, 16, 18, new Color(80, 215, 100, 255));
                    Raylib.DrawText($"{profile.Gold} G", width - 215, 16, 20, Color.Gold);
                    Raylib.DrawText($"{profile.SkillPoints} SP", width - 100, 16, 20, Color.SkyBlue);

                    if (DrawButton(new Rectangle(40, 74, 160, 38), "DECK BUILDER", new Color(58, 70, 92, 255), new Color(78, 98, 130, 255)))
                        scene = GameScene.DeckBuilder;
                    if (DrawButton(new Rectangle(212, 74, 165, 38), "MARKET  /  SKILLS", new Color(82, 62, 42, 255), new Color(120, 92, 62, 255)))
                        scene = GameScene.MarketShop;

                    int cmHdr = Raylib.MeasureText("SECTOR MAP", 20);
                    Raylib.DrawText("SECTOR MAP", width / 2 - cmHdr / 2, 215, 20, new Color(115, 120, 142, 255));

                    // Which nodes can be fought next
                    var availableIds = new HashSet<int>();
                    if (completedNodes.Count == 0) availableIds.Add(0);
                    foreach (var cn2 in campaignNodes)
                        if (completedNodes.Contains(cn2.Id))
                            foreach (var cid in cn2.ChildIds)
                                if (!completedNodes.Contains(cid)) availableIds.Add(cid);

                    // Node screen position helper
                    int MapNX(CampaignNode n) => (int)(80 + n.MapX * (width - 160));
                    int MapNY(CampaignNode n) => (int)(250 + n.MapY * (height - 380));

                    // Draw connector lines first
                    foreach (var cn2 in campaignNodes)
                    {
                        int px = MapNX(cn2), py = MapNY(cn2);
                        foreach (var cid in cn2.ChildIds)
                        {
                            var child = campaignNodes.Find(x => x.Id == cid)!;
                            int cx2 = MapNX(child), cy2 = MapNY(child);
                            Color lc = completedNodes.Contains(cn2.Id) && completedNodes.Contains(cid)
                                ? new Color(45, 130, 65, 220)
                                : completedNodes.Contains(cn2.Id) && availableIds.Contains(cid)
                                    ? new Color(170, 130, 40, 180)
                                    : new Color(38, 42, 54, 255);
                            Raylib.DrawLineEx(new Vector2(px, py), new Vector2(cx2, cy2), 2.5f, lc);
                        }
                    }

                    // Draw nodes
                    foreach (var node in campaignNodes)
                    {
                        int nodeX = MapNX(node), nodeY = MapNY(node);
                        bool isAvail   = availableIds.Contains(node.Id);
                        bool nodeHov   = Raylib.CheckCollisionPointRec(mouse, new Rectangle(nodeX - 85, nodeY - 55, 170, 110)) && isAvail;

                        Color nodeBase = node.Completed ? new Color(28, 80, 42, 255)
                                       : isAvail ? (node.Type == NodeType.CombatBoss ? new Color(95, 28, 28, 255) : new Color(85, 42, 18, 255))
                                       : new Color(18, 20, 28, 255);
                        Color nodeHovC = node.Completed ? new Color(38, 105, 55, 255)
                                       : node.Type == NodeType.CombatBoss ? new Color(135, 38, 38, 255) : new Color(120, 60, 22, 255);
                        Color nodeBord = node.Completed ? new Color(55, 185, 80, 200)
                                       : isAvail ? (node.Type == NodeType.CombatBoss ? new Color(210, 55, 55, 200) : new Color(218, 130, 40, 200))
                                       : new Color(38, 40, 52, 180);

                        Rectangle nodeBox = new(nodeX - 85, nodeY - 55, 170, 110);
                        Raylib.DrawRectangleRounded(nodeBox, 0.1f, 6, nodeHov ? nodeHovC : nodeBase);
                        Raylib.DrawLineEx(new Vector2(nodeBox.X + 6, nodeBox.Y + 1.5f), new Vector2(nodeBox.X + nodeBox.Width - 6, nodeBox.Y + 1.5f), 1f, new Color(255, 255, 255, 28));
                        Raylib.DrawRectangleRoundedLinesEx(nodeBox, 0.1f, 6, 1.5f, nodeBord);

                        if (!isAvail && !node.Completed)
                            Raylib.DrawRectangleRounded(nodeBox, 0.1f, 6, new Color(0, 0, 0, 120));

                        string typeTag = node.Type switch { NodeType.CombatElite => "ELITE", NodeType.CombatBoss => "BOSS", _ => "SECTOR" };
                        Color typeCol  = node.Type switch { NodeType.CombatElite => new Color(215, 168, 38, 255), NodeType.CombatBoss => new Color(210, 55, 55, 255), _ => new Color(120, 175, 125, 255) };
                        int ttw = Raylib.MeasureText(typeTag, 11);
                        Raylib.DrawText(typeTag, nodeX - ttw / 2, nodeY - 46, 11, typeCol);

                        string displayName = node.Name.Length > 16 ? node.Name[..15] + "…" : node.Name;
                        int dnw = Raylib.MeasureText(displayName, 12);
                        Raylib.DrawText(displayName, nodeX - dnw / 2, nodeY - 22, 12, node.Completed ? new Color(145, 210, 155, 255) : Color.White);

                        string hpStr = $"HP  {node.EnemyHp}";
                        int hpw = Raylib.MeasureText(hpStr, 11);
                        Raylib.DrawText(hpStr, nodeX - hpw / 2, nodeY + 2, 11, new Color(190, 100, 100, 255));

                        if (node.Completed)
                        {
                            int ckw = Raylib.MeasureText("CLEARED", 11);
                            Raylib.DrawText("CLEARED", nodeX - ckw / 2, nodeY + 22, 11, new Color(75, 200, 95, 255));
                        }
                        else if (isAvail)
                        {
                            int ffw = Raylib.MeasureText("CLICK TO FIGHT", 10);
                            Raylib.DrawText("CLICK TO FIGHT", nodeX - ffw / 2, nodeY + 22, 10, new Color(218, 130, 40, 220));
                        }

                        if (nodeHov && Raylib.IsMouseButtonPressed(MouseButton.Left))
                        {
                            activeBattleNode = node;
                            battleService.NewBattle();
                            battleService.State.DrawPile.Clear();
                            battleService.State.Hand.Clear();
                            battleService.State.BurnPile.Clear();

                            int finalMaxHp = 50 + profile.MaxHpBonus;
                            battleService.State.Player.MaxHp = finalMaxHp;
                            int startHp = profile.PlayerHp > 0 ? Math.Min(profile.PlayerHp, finalMaxHp) : finalMaxHp;
                            battleService.State.Player.Hp    = startHp;
                            battleService.State.Enemy.MaxHp  = node.EnemyHp;
                            battleService.State.Enemy.Hp     = node.EnemyHp;
                            battleService.State.Enemy.Position = node.EnemyDefaultPosition;

                            List<CardDefinition> gameDeckCopy = new(profile.ActiveDeck);
                            for (int k = gameDeckCopy.Count - 1; k > 0; k--)
                            {
                                int idx = battleService.State.Rng.Next(k + 1);
                                (gameDeckCopy[k], gameDeckCopy[idx]) = (gameDeckCopy[idx], gameDeckCopy[k]);
                            }
                            battleService.State.DrawPile.AddRange(gameDeckCopy);
                            battleService.State.PlayerEnergyBonus = profile.MaxEnergyBonus;
                            battleService.State.EquippedAbility   = profile.SelectedAbility;
                            battleService.State.Player.Energy     = 4 + profile.MaxEnergyBonus;
                            new TurnManager().DrawToHand(battleService.State);
                            // Set enemy variant by node type
                            battleService.State.EnemyVariant = node.Type switch {
                                NodeType.CombatElite => "elite",
                                NodeType.CombatBoss  => "boss",
                                _                    => "standard"
                            };
                            battleEndSoundPlayed = false;
                            prevAbilityCharge    = 0f;
                            showBurnPile   = false;
                            confirmRestart = false;
                            displayedEnemyHp  = -1f;
                            displayedPlayerHp = -1f;
                            enemyBobTime = 0f;
                            floatDmgs.Clear();
                            Array.Clear(slotFlashTimers, 0, 5);
                            Array.Clear(prevSlotOccupied, 0, 5);
                            turnBannerText = "YOUR TURN";
                            turnBannerTimer = 1.8f;
                            Raylib.PlaySound(ui1Sound);
                            scene = GameScene.BattleView;
                        }
                    }
                    break;

                case GameScene.BattleView:
                    var bs = battleService.State;

                    // Detect unit deaths (slot went occupied → empty this frame)
                    for (int si = 0; si < 5; si++)
                    {
                        bool occ = bs.PlayerBoard[si].IsOccupied;
                        if (prevSlotOccupied[si] && !occ) slotFlashTimers[si] = 0.3f;
                        prevSlotOccupied[si] = occ;
                    }

                    // Consume damage log → spawn float numbers + trigger flash + shake + play hit sounds
                    if (bs.DamageLog.Count > 0)
                    {
                        bool hitEnemy  = bs.DamageLog.Any(e => e.Amount > 0 && e.Tag.StartsWith("enemy"));
                        bool hitPlayer = bs.DamageLog.Any(e => e.Amount < 0);
                        if (hitEnemy)  { enemyFlashTimer = 0.20f; shakeTimer = 0.18f; Raylib.PlaySound(hitSounds[sfxRng.Next(4)]); }
                        if (hitPlayer) { shakeTimer = 0.25f; Raylib.PlaySound(hitSounds[sfxRng.Next(4)]); }
                        foreach (var ev in bs.DamageLog)
                        {
                            bool isEnemyHit = ev.Amount > 0;
                            Color fc = isEnemyHit ? new Color(255, 110, 50, 255) : new Color(215, 50, 50, 255);
                            if (ev.Tag == "enemy_burn") fc = new Color(255, 165, 30, 255);
                            if (ev.Tag == "enemy_bio")  fc = new Color(80, 210, 60, 255);
                            Vector2 sp = ev.Tag == "player"
                                ? new Vector2(width * 0.12f + rewardRng.Next(-18, 18), height * 0.52f)
                                : new Vector2(enemyScreenPos.X + rewardRng.Next(-22, 22), enemyScreenPos.Y - 8);
                            floatDmgs.Add(new FloatDmg { X = sp.X, Y = sp.Y, Vy = -62f,
                                Amount = Math.Abs(ev.Amount), Life = 1.0f, Col = fc });
                        }
                        bs.DamageLog.Clear();
                    }
                    // Update float damage particles
                    for (int fi = floatDmgs.Count - 1; fi >= 0; fi--)
                    {
                        var fd = floatDmgs[fi];
                        fd.Y  += fd.Vy * dt;
                        fd.Vy *= 0.93f;
                        fd.Life -= dt * 0.85f;
                        floatDmgs[fi] = fd;
                        if (fd.Life <= 0) floatDmgs.RemoveAt(fi);
                    }
                    if (enemyFlashTimer > 0) enemyFlashTimer -= dt;

                    // Ability charged ping (Whoosh_6) — plays once when bar fills
                    if (bs.EquippedAbility != null)
                    {
                        if (prevAbilityCharge >= 0f && prevAbilityCharge < bs.EquippedAbility.MaxCharge
                            && bs.AbilityCharge >= bs.EquippedAbility.MaxCharge)
                            Raylib.PlaySound(whoosh6);
                        prevAbilityCharge = bs.AbilityCharge;
                    }

                    if (bs.Phase == TurnPhase.PlayerTurn && !isPaused)
                    {
                        if (Raylib.IsKeyPressed(KeyboardKey.Enter))
                        {
                            battleService.EndTurn();
                            Raylib.PlaySound(ui1Sound);
                            if (bs.Phase != TurnPhase.Finished) { turnBannerText = "YOUR TURN"; turnBannerTimer = 1.4f; }
                        }
                        if (Raylib.IsKeyPressed(KeyboardKey.Space) && bs.SelectedBoardSlot >= 0)
                            battleService.ExecuteCard(bs.SelectedBoardSlot);
                        if (Raylib.IsKeyPressed(KeyboardKey.Q))
                            if (battleService.ActivateAbility()) Raylib.PlaySound(whoosh7);
                    }

                    if (theme == GameTheme.SciFi)
                    {
                        // === SCI-FI: 3-column layout, grid always visible ===
                        const int SW = 220;
                        int gcx = SW, gcw = width - SW * 2;
                        int cpH = 72, cpW = SW - 20;

                        int hovSF = -1;
                        if (!dragging)
                            for (int j = 0; j < bs.Hand.Count; j++)
                            {
                                Rectangle hcr = new Rectangle(10, 74 + j * (cpH + 6), cpW, cpH);
                                if (Raylib.CheckCollisionPointRec(mouse, hcr)) { hovSF = j; break; }
                            }

                        // 8-bit palette constants
                        Color px8Bg   = new Color(2,   4,   2,   255); // near-black green
                        Color px8Grn  = new Color(0,   255, 65,  255); // NES green
                        Color px8GrnD = new Color(0,   80,  20,  255); // dark green
                        Color px8Cyn  = new Color(0,   220, 220, 255); // cyan
                        Color px8Red  = new Color(255, 30,  30,  255); // red
                        Color px8Yel  = new Color(255, 215, 0,   255); // yellow
                        Color px8Gry  = new Color(90,  95,  90,  255); // gray

                        // Left sidebar: Player (8-bit)
                        Raylib.DrawRectangle(0, 0, SW, height, px8Bg);
                        Raylib.DrawRectangle(0, 0, SW, 68, new Color(0, 18, 5, 255)); // header panel
                        Raylib.DrawLine(SW, 0, SW, height, px8GrnD);
                        Raylib.DrawRectangle(0, 0, SW, 2, px8Grn);
                        Raylib.DrawRectangle(0, 2, SW, 1, new Color(0, 100, 30, 60));
                        Raylib.DrawText("▶ PLAYER", 10, 8, 11, new Color(0, 155, 40, 255));
                        Raylib.DrawText($"HP  {bs.Player.Hp} / {bs.Player.MaxHp}", 10, 22, 13, px8Grn);
                        // HP bar with lerped fill + ghost
                        DrawBar(10, 38, SW - 22, 6, (float)bs.Player.Hp / Math.Max(bs.Player.MaxHp, 1), new Color(0, 50, 15, 255), new Color(0, 8, 2, 255));
                        DrawBar(10, 38, SW - 22, 6, Math.Max(0f, displayedPlayerHp) / Math.Max(bs.Player.MaxHp, 1), px8Grn, Color.Blank);
                        // Energy pips
                        int maxEnPips = Math.Max(bs.Player.Energy, TurnManager.BaseEnergyPerTurn + bs.PlayerEnergyBonus);
                        maxEnPips = Math.Min(maxEnPips, 10);
                        for (int ei = 0; ei < maxEnPips; ei++)
                        {
                            bool avail = ei < bs.Player.Energy;
                            Raylib.DrawCircleV(new Vector2(14 + ei * 11, 54), 4f, avail ? px8Cyn : new Color(0, 35, 40, 255));
                            if (avail) Raylib.DrawCircleLines(14 + ei * 11, 54, 4, new Color(80, 255, 255, 50));
                        }
                        bool disHovSF = Raylib.CheckCollisionPointRec(mouse, new Rectangle(107, 47, 80, 16));
                        Raylib.DrawText($"DIS {bs.BurnPile.Count}", 110, 48, 12, disHovSF ? Color.White : px8Yel);
                        if (!isPaused && disHovSF && Raylib.IsMouseButtonPressed(MouseButton.Left))
                            { showBurnPile = !showBurnPile; dragging = false; draggingIndex = -1; }
                        Raylib.DrawLine(0, 66, SW, 66, px8GrnD);

                        for (int i = 0; i < bs.Hand.Count; i++)
                        {
                            if (dragging && i == draggingIndex) continue;
                            Rectangle cr = new Rectangle(10, 74 + i * (cpH + 6), cpW, cpH);
                            var hc = bs.Hand[i];
                            bool hov8 = i == hovSF;
                            // Card with element accent strip
                            Color elCol = CardColorFromName(hc.Name);
                            Raylib.DrawRectangleRec(cr, new Color((byte)(elCol.R / 10), (byte)(elCol.G / 10), (byte)(elCol.B / 10), (byte)255));
                            Raylib.DrawRectangleLinesEx(cr, hov8 ? 2 : 1, hov8 ? px8Yel : new Color((byte)(elCol.R / 3), (byte)(elCol.G / 3), (byte)(elCol.B / 3), (byte)180));
                            Raylib.DrawRectangle((int)cr.X + 1, (int)cr.Y + 1, (int)cr.Width - 2, 4, new Color((byte)(elCol.R / 2), (byte)(elCol.G / 2), (byte)(elCol.B / 2), (byte)200));
                            Raylib.DrawText(hc.Name, (int)cr.X + 6, (int)cr.Y + 8, 11, hov8 ? px8Yel : Color.White);
                            Raylib.DrawLine((int)cr.X, (int)cr.Y + 23, (int)(cr.X + cr.Width), (int)cr.Y + 23, new Color((byte)(elCol.R / 5), (byte)(elCol.G / 5), (byte)(elCol.B / 5), (byte)140));
                            Raylib.DrawText(hc.Description, (int)cr.X + 6, (int)cr.Y + 27, 9, new Color(155, 162, 175, 255));
                            Raylib.DrawCircleV(new Vector2(cr.X + cr.Width - 14, cr.Y + 14), 11f, new Color(0, 40, 90, 255));
                            Raylib.DrawCircleLines((int)(cr.X + cr.Width - 14), (int)(cr.Y + 14), 11, new Color(40, 90, 200, 160));
                            int c8W = Raylib.MeasureText($"{hc.Cost}", 11);
                            Raylib.DrawText($"{hc.Cost}", (int)(cr.X + cr.Width - 14 - c8W / 2), (int)(cr.Y + 8), 11, Color.White);
                            if (!isPaused && !showBurnPile && Raylib.IsMouseButtonPressed(MouseButton.Left) && Raylib.CheckCollisionPointRec(mouse, cr) && !dragging)
                            {
                                dragging = true; draggingIndex = i; isDraggingFromCollectionPool = false;
                                dragOffset = mouse - new Vector2(cr.X, cr.Y);
                            }
                        }

                        int sbBot = height - 54;
                        Raylib.DrawLine(0, sbBot, SW, sbBot, px8GrnD);
                        Raylib.DrawText($"HAND:{bs.Hand.Count}", 10, sbBot + 8, 11, px8Gry);
                        Raylib.DrawText($"DECK:{bs.DrawPile.Count}", 10, sbBot + 24, 11, px8Gry);

                        // Right sidebar: Enemy / Board (8-bit)
                        Raylib.DrawRectangle(width - SW, 0, SW, height, px8Bg);
                        Raylib.DrawRectangle(width - SW, 0, SW, 74, new Color(22, 3, 3, 255)); // header panel
                        Raylib.DrawLine(width - SW, 0, width - SW, height, new Color(60, 0, 0, 255));
                        Raylib.DrawRectangle(width - SW, 0, SW, 2, px8Red);
                        Raylib.DrawRectangle(width - SW, 2, SW, 1, new Color(120, 20, 20, 60));
                        int enw2 = Raylib.MeasureText(activeBattleNode?.Name ?? "???", 11);
                        Raylib.DrawText(activeBattleNode?.Name ?? "???", width - SW + (SW - enw2) / 2, 8, 11, px8Red);
                        Raylib.DrawText($"HP  {bs.Enemy.Hp} / {bs.Enemy.MaxHp}", width - SW + 8, 22, 13, new Color(255, 80, 80, 255));
                        // HP bar with lerped fill
                        DrawBar(width - SW + 8, 38, SW - 22, 6, (float)bs.Enemy.Hp / Math.Max(bs.Enemy.MaxHp, 1), new Color(60, 6, 6, 255), new Color(12, 2, 2, 255));
                        DrawBar(width - SW + 8, 38, SW - 22, 6, Math.Max(0f, displayedEnemyHp) / Math.Max(bs.Enemy.MaxHp, 1), px8Red, Color.Blank);
                        int fS  = bs.Enemy.ActiveElements.GetStacks(ElementType.Fire);
                        int frS = bs.Enemy.ActiveElements.GetStacks(ElementType.Frost);
                        int biS = bs.Enemy.ActiveElements.GetStacks(ElementType.Bio);
                        if (fS  > 0) Raylib.DrawText($"FIRE:{fS}",  width - SW + 8,   48, 11, new Color(255, 140, 0, 255));
                        if (frS > 0) Raylib.DrawText($"ICE:{frS}",  width - SW + 68,  48, 11, new Color(80, 220, 255, 255));
                        if (biS > 0) Raylib.DrawText($"BIO:{biS}",  width - SW + 128, 48, 11, new Color(60, 210, 80, 255));
                        // Status tooltips
                        bool sf8FireHov  = fS  > 0 && Raylib.CheckCollisionPointRec(mouse, new Rectangle(width - SW + 4,   42, 60, 20));
                        bool sf8FrostHov = frS > 0 && Raylib.CheckCollisionPointRec(mouse, new Rectangle(width - SW + 64,  42, 60, 20));
                        bool sf8BioHov   = biS > 0 && Raylib.CheckCollisionPointRec(mouse, new Rectangle(width - SW + 124, 42, 60, 20));
                        if (sf8FireHov || sf8FrostHov || sf8BioHov)
                        {
                            string stTitle = sf8FireHov  ? $"FIRE x{fS}"  : sf8FrostHov ? $"ICE x{frS}"  : $"BIO x{biS}";
                            string stBody  = sf8FireHov  ? "Burns 2 dmg/stack each turn, -1 stack/turn"
                                           : sf8FrostHov ? "Reduces enemy attack, -1 stack/turn"
                                           :               "Poisons 1 dmg/stack each turn, -1 stack/turn";
                            Color stAccent = sf8FireHov  ? new Color(255, 140, 0, 200)
                                           : sf8FrostHov ? new Color(80, 220, 255, 200)
                                           :               new Color(60, 210, 80, 200);
                            int stW = 230, stH = 64;
                            int stX = width - SW - stW - 6, stY = 38;
                            Raylib.DrawRectangleRounded(new Rectangle(stX, stY, stW, stH), 0.12f, 6, new Color(10, 12, 20, 248));
                            Raylib.DrawRectangleRoundedLinesEx(new Rectangle(stX, stY, stW, stH), 0.12f, 6, 1.2f, stAccent);
                            Raylib.DrawText(stTitle, stX + 10, stY + 10, 13, stAccent);
                            Raylib.DrawText(stBody,  stX + 10, stY + 34, 11, new Color(170, 175, 190, 255));
                        }
                        // Enemy intent
                        {
                            var (intentSF, dmgSF, laneSF) = TurnManager.GetEnemyIntent(bs);
                            int inW = Raylib.MeasureText(intentSF, 9);
                            int inX = width - SW + Math.Max(0, (SW - inW) / 2);
                            Raylib.DrawText(intentSF, inX, 62, 9, new Color(255, 120, 60, 220));
                        }
                        Raylib.DrawLine(width - SW, 74, width, 74, new Color(60, 0, 0, 255));

                        for (int i = 0; i < bs.PlayerBoard.Count; i++)
                        {
                            var bSlot = bs.PlayerBoard[i];
                            bool bSel = bs.SelectedBoardSlot == i;
                            Rectangle sr = new(width - SW + 10, 80 + i * (cpH + 6), cpW, cpH);
                            if (bSlot.IsOccupied)
                            {
                                var occ = bSlot.Occupant!;
                                Color uc = CardColorFromName(occ.SourceCard.Name);
                                Raylib.DrawRectangleRec(sr, new Color(0, 0, 0, 255));
                                Raylib.DrawRectangleLinesEx(sr, bSel ? 3 : 2, bSel ? px8Yel : uc);
                                Raylib.DrawText(occ.SourceCard.Name, (int)sr.X + 5, (int)sr.Y + 5, 11, bSel ? px8Yel : uc);
                                Raylib.DrawText($"ATK {occ.BaseAttack}  HP {occ.Hp}", (int)sr.X + 5, (int)sr.Y + 22, 10, px8Gry);
                                DrawBar((int)sr.X + 5, (int)sr.Y + 38, (int)sr.Width - 10, 4,
                                    (float)occ.Hp / Math.Max(occ.MaxHp, 1), uc, new Color(20, 20, 20, 255));
                            }
                            else
                            {
                                Raylib.DrawRectangleRec(sr, new Color(4, 4, 4, 255));
                                Raylib.DrawRectangleLinesEx(sr, 1, bSel ? px8Yel : new Color(25, 25, 25, 255));
                                Raylib.DrawText($"LANE {i}", (int)sr.X + 8, (int)(sr.Y + sr.Height / 2 - 7), 12, new Color(30, 30, 30, 255));
                            }
                            if (!isPaused && Raylib.IsMouseButtonPressed(MouseButton.Left) && Raylib.CheckCollisionPointRec(mouse, sr))
                                bs.SelectedBoardSlot = bSel ? -1 : i;
                            // Death flash
                            if (slotFlashTimers[i] > 0)
                            {
                                byte fa8 = (byte)Math.Min(200, (int)(slotFlashTimers[i] / 0.3f * 200));
                                Raylib.DrawRectangleRec(sr, new Color((byte)255, (byte)255, (byte)255, fa8));
                            }
                        }

                        // 8-bit center grid (flat, sharp, neon)
                        int fTop = 70, fBot = height - 100;
                        int bL = gcx + 4, bR = gcx + gcw - 4;
                        int midY = fTop + (fBot - fTop) / 2;

                        // Grid background — pure black
                        Raylib.DrawRectangle(gcx, fTop, gcw, fBot - fTop, new Color(0, 0, 0, 255));
                        // Pixel grid lines
                        for (int gx = gcx; gx < gcx + gcw; gx += 20) Raylib.DrawLine(gx, fTop, gx, fBot, new Color(0, 28, 0, 255));
                        for (int gy = fTop; gy < fBot; gy += 20)      Raylib.DrawLine(gcx, gy, gcx + gcw, gy, new Color(0, 28, 0, 255));
                        // Bright border
                        Raylib.DrawRectangleLinesEx(new Rectangle(gcx, fTop, gcw, fBot - fTop), 2, px8GrnD);
                        // Mid divider
                        Raylib.DrawLine(bL, midY, bR, midY, new Color(0, 90, 0, 255));

                        int ezLW = Raylib.MeasureText("▲ ENEMY ZONE", 10);
                        Raylib.DrawText("▲ ENEMY ZONE", gcx + (gcw - ezLW) / 2, fTop + 4, 10, new Color(200, 0, 0, 180));
                        int dzLW = Raylib.MeasureText("▼ DEPLOY ZONE", 10);
                        Raylib.DrawText("▼ DEPLOY ZONE", gcx + (gcw - dzLW) / 2, fBot - 16, 10, new Color(0, 180, 0, 180));

                        for (int i = 0; i < 5; i++)
                        {
                            int px0 = bL + i * (bR - bL) / 5 + 3;
                            int px1 = bL + (i + 1) * (bR - bL) / 5 - 3;

                            Rectangle dropZone  = new Rectangle(px0, midY + 3, px1 - px0, fBot - midY - 6);
                            Rectangle enemyZone = new Rectangle(px0, fTop + 3, px1 - px0, midY - fTop - 6);

                            bool isTarget = dragging && Raylib.CheckCollisionPointRec(mouse, dropZone);
                            // Deploy slot
                            Raylib.DrawRectangleLinesEx(dropZone, isTarget ? 2 : 1, isTarget ? px8Yel : new Color(0, 45, 0, 255));
                            if (isTarget) Raylib.DrawRectangleRec(new Rectangle(dropZone.X + 1, dropZone.Y + 1, dropZone.Width - 2, dropZone.Height - 2), new Color(255, 215, 50, 20));

                            bool hasEnemy = bs.Enemy.Position == i;
                            if (hasEnemy)
                            {
                                // Bob + pulse animation
                                float bobYOff = MathF.Sin(enemyBobTime * 2.4f) * 4f;
                                float pulse   = (MathF.Sin(enemyBobTime * 3.8f) + 1f) * 0.5f;
                                Rectangle bz  = new Rectangle(enemyZone.X, enemyZone.Y + bobYOff, enemyZone.Width, enemyZone.Height);

                                // Status rings
                                if (bs.Enemy.ActiveElements.GetStacks(ElementType.Fire)  > 0)
                                    Raylib.DrawRectangleLinesEx(new Rectangle(bz.X - 2, bz.Y - 2, bz.Width + 4, bz.Height + 4), 2, new Color(255, 140, 0, 255));
                                if (bs.Enemy.ActiveElements.GetStacks(ElementType.Frost) > 0)
                                    Raylib.DrawRectangleLinesEx(new Rectangle(bz.X - 4, bz.Y - 4, bz.Width + 8, bz.Height + 8), 2, new Color(80, 220, 255, 255));

                                // Body
                                Raylib.DrawRectangleRec(bz, new Color(90, 5, 5, 255));
                                // Scanlines
                                for (int sy2 = (int)bz.Y + 1; sy2 < bz.Y + bz.Height - 1; sy2 += 3)
                                    Raylib.DrawLine((int)bz.X + 1, sy2, (int)(bz.X + bz.Width) - 1, sy2, new Color(0, 0, 0, 45));
                                // Pulsing border
                                int glB = 60 + (int)(pulse * 90);
                                Raylib.DrawRectangleLinesEx(bz, 2, px8Red);
                                Raylib.DrawRectangleLinesEx(new Rectangle(bz.X - 1, bz.Y - 1, bz.Width + 2, bz.Height + 2), 1, new Color(255, 80, 80, glB));

                                // Face
                                int ecx8 = (int)(bz.X + bz.Width * 0.5f);
                                int ecy8 = (int)(bz.Y + bz.Height * 0.38f);
                                int eyeB = 175 + (int)(pulse * 80);
                                // Glowing eyes
                                Raylib.DrawCircleV(new Vector2(ecx8 - 11, ecy8), 7f, new Color(255, eyeB, 15, 255));
                                Raylib.DrawCircleV(new Vector2(ecx8 + 11, ecy8), 7f, new Color(255, eyeB, 15, 255));
                                Raylib.DrawCircleV(new Vector2(ecx8 - 11, ecy8), 3f, new Color(0, 0, 0, 255));
                                Raylib.DrawCircleV(new Vector2(ecx8 + 11, ecy8), 3f, new Color(0, 0, 0, 255));
                                // Visor / mouth bar
                                int visA = 140 + (int)(pulse * 70);
                                Raylib.DrawRectangle(ecx8 - 17, ecy8 + 12, 34, 5, new Color(255, 55, 55, visA));
                                Raylib.DrawRectangleLinesEx(new Rectangle(ecx8 - 17, ecy8 + 12, 34, 5), 1, new Color(255, 130, 130, 190));
                                // Corner tech brackets
                                int bsz2 = 5;
                                Raylib.DrawLine((int)bz.X + 2, (int)bz.Y + 2, (int)bz.X + 2 + bsz2, (int)bz.Y + 2, px8Red);
                                Raylib.DrawLine((int)bz.X + 2, (int)bz.Y + 2, (int)bz.X + 2, (int)bz.Y + 2 + bsz2, px8Red);
                                Raylib.DrawLine((int)(bz.X + bz.Width) - 2, (int)bz.Y + 2, (int)(bz.X + bz.Width) - 2 - bsz2, (int)bz.Y + 2, px8Red);
                                Raylib.DrawLine((int)(bz.X + bz.Width) - 2, (int)bz.Y + 2, (int)(bz.X + bz.Width) - 2, (int)bz.Y + 2 + bsz2, px8Red);

                                int ehpW2 = Raylib.MeasureText($"{bs.Enemy.Hp}", 11);
                                Raylib.DrawText($"{bs.Enemy.Hp}", ecx8 - ehpW2 / 2, (int)(bz.Y + bz.Height - 14), 11, px8Yel);
                                if (enemyFlashTimer > 0)
                                {
                                    byte fa = (byte)Math.Min(200, (int)(enemyFlashTimer * 5 * 255));
                                    Raylib.DrawRectangleRec(bz, new Color((byte)255, (byte)255, (byte)255, fa));
                                }
                                enemyScreenPos = new Vector2(bz.X + bz.Width / 2f, bz.Y + bz.Height / 2f);
                            }
                            else
                            {
                                Raylib.DrawRectangleLinesEx(enemyZone, 1, new Color(30, 0, 0, 255));
                            }

                            if (i < bs.PlayerBoard.Count && bs.PlayerBoard[i].IsOccupied)
                            {
                                var occ = bs.PlayerBoard[i].Occupant!;
                                bool uSel = bs.SelectedBoardSlot == i;
                                Color uc8 = CardColorFromName(occ.SourceCard.Name);
                                // 8-bit unit block
                                Raylib.DrawRectangleRec(new Rectangle(dropZone.X + 2, dropZone.Y + 2, dropZone.Width - 4, dropZone.Height - 4), new Color(uc8.R / 6, uc8.G / 6, uc8.B / 6, 255));
                                Raylib.DrawRectangleLinesEx(new Rectangle(dropZone.X + 2, dropZone.Y + 2, dropZone.Width - 4, dropZone.Height - 4), uSel ? 3 : 2, uSel ? px8Yel : uc8);
                                int unW8 = Raylib.MeasureText(occ.SourceCard.Name, 10);
                                Raylib.DrawText(occ.SourceCard.Name, (int)(dropZone.X + (dropZone.Width - unW8) / 2), (int)(dropZone.Y + 6), 10, uSel ? px8Yel : uc8);
                                int aLblW = Raylib.MeasureText($"ATK{occ.BaseAttack}", 10);
                                Raylib.DrawText($"ATK{occ.BaseAttack}", (int)(dropZone.X + (dropZone.Width - aLblW) / 2), (int)(dropZone.Y + dropZone.Height - 18), 10, px8Cyn);
                                DrawBar(dropZone.X + 4, dropZone.Y + dropZone.Height - 8, dropZone.Width - 8, 4,
                                    (float)occ.Hp / Math.Max(occ.MaxHp, 1), px8Grn, new Color(0, 20, 0, 255));
                            }
                            // Death flash on grid slot
                            if (i < 5 && slotFlashTimers[i] > 0)
                            {
                                byte fg8 = (byte)Math.Min(200, (int)(slotFlashTimers[i] / 0.3f * 200));
                                Raylib.DrawRectangleRec(dropZone, new Color((byte)255, (byte)255, (byte)255, fg8));
                            }
                        }

                        // Drop release
                        if (!isPaused && !Raylib.IsMouseButtonDown(MouseButton.Left) && dragging && !isDraggingFromCollectionPool)
                        {
                            for (int i = 0; i < 5; i++)
                            {
                                float t0 = (float)i / 5, t1 = (float)(i + 1) / 5;
                                int px0d = (int)(bL + t0 * (bR - bL)) + 4, px1d = (int)(bL + t1 * (bR - bL)) - 4;
                                Rectangle dz = new Rectangle(px0d, midY + 4, px1d - px0d, fBot - midY - 8);
                                if (Raylib.CheckCollisionPointRec(mouse, dz)) { if (battleService.PlaceCard(draggingIndex, i)) Raylib.PlaySound(whooshSounds[sfxRng.Next(5)]); break; }
                            }
                            dragging = false; draggingIndex = -1;
                        }

                        // Hover preview (overlays grid, not blocking drag)
                        if (hovSF >= 0 && !dragging)
                        {
                            Raylib.DrawRectangle(gcx, 0, gcw, height, new Color(8, 9, 14, 155));
                            var pv = bs.Hand[hovSF];
                            int pvW = 240, pvH = 320;
                            Rectangle pvRect = new Rectangle(gcx + (gcw - pvW) / 2, height / 2 - pvH / 2, pvW, pvH);
                            DrawCard(pvRect, pv.Name, pv.Description, pv.Cost, new Color(20, 24, 34, 255), Color.Gold);
                            int pwlW = Raylib.MeasureText("drag to play", 14);
                            Raylib.DrawText("drag to play", (int)(pvRect.X + (pvW - pwlW) / 2), (int)(pvRect.Y + pvH + 10), 14, Color.Gray);
                            if (!isPaused && Raylib.IsMouseButtonPressed(MouseButton.Left) && Raylib.CheckCollisionPointRec(mouse, pvRect))
                            {
                                dragging = true; draggingIndex = hovSF; isDraggingFromCollectionPool = false;
                                dragOffset = mouse - new Vector2(pvRect.X, pvRect.Y);
                            }
                        }

                        // 8-bit phase indicator + END TURN
                        bool isPlayerTurnSF = bs.Phase == TurnPhase.PlayerTurn;
                        string phaseLblSF = isPlayerTurnSF ? "> YOUR TURN <" : "! ENEMY TURN !";
                        Color phaseColSF = isPlayerTurnSF ? px8Grn : px8Red;
                        int phWSF = Raylib.MeasureText(phaseLblSF, 13);
                        Raylib.DrawRectangle(gcx, height - 108, gcw, 22, new Color(0, 0, 0, 200));
                        Raylib.DrawLine(gcx, height - 108, gcx + gcw, height - 108, isPlayerTurnSF ? px8GrnD : new Color(60, 0, 0, 255));
                        Raylib.DrawText(phaseLblSF, gcx + gcw / 2 - phWSF / 2, height - 104, 13, phaseColSF);
                        if (isPlayerTurnSF && !isPaused && DrawButton(new Rectangle(gcx + gcw / 2 - 88, height - 83, 176, 36), "[ END TURN ]", new Color(0, 30, 0, 255), new Color(0, 55, 0, 255)))
                        {
                            battleService.EndTurn(); Raylib.PlaySound(ui1Sound);
                            if (bs.Phase != TurnPhase.Finished) { turnBannerText = "YOUR TURN"; turnBannerTimer = 1.4f; }
                        }

                        // Ability bar
                        if (bs.EquippedAbility != null)
                        {
                            if (DrawAbilityBar(gcx + 8, height - 44, gcw - 16, 30, bs, isPlayerTurnSF))
                                if (battleService.ActivateAbility()) Raylib.PlaySound(whoosh7);
                        }

                        // Scanlines overlay (full SciFi screen)
                        for (int sly = 0; sly < height; sly += 4)
                            Raylib.DrawRectangle(0, sly, width, 1, new Color(0, 0, 0, 40));

                        // Drag ghost (8-bit card)
                        if (dragging && draggingIndex >= 0 && draggingIndex < bs.Hand.Count)
                        {
                            var dc = bs.Hand[draggingIndex];
                            float gx2 = mouse.X - dragOffset.X, gy2 = mouse.Y - dragOffset.Y;
                            Raylib.DrawRectangle((int)gx2, (int)gy2, 140, 188, new Color(0, 15, 0, 230));
                            Raylib.DrawRectangleLinesEx(new Rectangle(gx2, gy2, 140, 188), 2, px8Yel);
                            Raylib.DrawText(dc.Name, (int)gx2 + 6, (int)gy2 + 6, 12, px8Yel);
                            Raylib.DrawLine((int)gx2, (int)gy2 + 22, (int)gx2 + 140, (int)gy2 + 22, px8GrnD);
                            Raylib.DrawText(dc.Description, (int)gx2 + 6, (int)gy2 + 28, 10, px8Gry);
                            Raylib.DrawText($"[{dc.Cost}]", (int)gx2 + 112, (int)gy2 + 6, 12, px8Cyn);
                        }
                    }
                    else
                    {
                        // === FANTASY: rotated 2.5D grid, dual hand piles ===
                        Color fpText  = new Color(212, 195, 162, 255);
                        Color fpPanel = new Color(34, 24, 14, 240);
                        Color fpBord  = new Color(105, 78, 42, 220);
                        Color fpAcct  = new Color(188, 148, 65, 255);

                        // Bottom-strip zones
                        Rectangle leftPileR  = new Rectangle(8,           height - 56, 110, 48);
                        Rectangle rightPileR = new Rectangle(width - 118, height - 56, 110, 48);
                        // Hand zone: pile button + card strip above it (490×210px bottom-left)
                        Rectangle leftHoverZone = new Rectangle(0, height - 210, 490, 210);
                        bool leftHov = Raylib.CheckCollisionPointRec(mouse, leftHoverZone);
                        // Field always visible; execute mode activates when over the battle area (not hand zone, not dragging)
                        int bTopApprox = (int)(height * 0.22f);
                        Rectangle fieldHoverZone = new Rectangle(0, bTopApprox, width, height - bTopApprox - 56);
                        bool rightHov = !leftHov && !dragging && Raylib.CheckCollisionPointRec(mouse, fieldHoverZone);
                        bool showGrid      = true;   // always visible
                        bool showHandCards = leftHov || dragging;
                        bool executeMode   = rightHov;

                        bool showFan = showHandCards; // alias for compat

                        // Header (0-54)
                        Raylib.DrawRectangle(0, 0, width, 54, new Color(22, 15, 8, 255));
                        Raylib.DrawLine(0, 54, width, 54, new Color(105, 78, 42, 180));
                        DrawBar(12, 14, 200, 10, (float)bs.Player.Hp / Math.Max(bs.Player.MaxHp, 1), new Color(20, 80, 30, 255), new Color(22, 30, 18, 255));
                        DrawBar(12, 14, 200, 10, Math.Max(0f, displayedPlayerHp) / Math.Max(bs.Player.MaxHp, 1), new Color(60, 200, 80, 255), Color.Blank);
                        int hpFW = Raylib.MeasureText($"{bs.Player.Hp}/{bs.Player.MaxHp}", 12);
                        Raylib.DrawText($"{bs.Player.Hp}/{bs.Player.MaxHp}", 12 + 100 - hpFW / 2, 28, 12, fpText);
                        Raylib.DrawText($"NRG {bs.Player.Energy}", 220, 14, 13, new Color(80, 185, 235, 255));
                        Raylib.DrawText($"DIS {bs.BurnPile.Count}", 220, 32, 12, new Color(205, 140, 48, 255));
                        // Header (0-54px)
                        Raylib.DrawRectangle(0, 0, width, 54, new Color(22, 15, 8, 255));
                        Raylib.DrawLine(0, 54, width, 54, new Color(105, 78, 42, 180));
                        DrawBar(12, 10, 180, 9, (float)bs.Player.Hp / Math.Max(bs.Player.MaxHp, 1), new Color(20, 80, 30, 255), new Color(22, 30, 18, 255));
                        DrawBar(12, 10, 180, 9, Math.Max(0f, displayedPlayerHp) / Math.Max(bs.Player.MaxHp, 1), new Color(60, 200, 80, 255), Color.Blank);
                        int fpHpFW = Raylib.MeasureText($"{bs.Player.Hp}/{bs.Player.MaxHp}", 11);
                        Raylib.DrawText($"{bs.Player.Hp}/{bs.Player.MaxHp}", 12 + 90 - fpHpFW / 2, 23, 11, fpText);
                        Raylib.DrawText($"NRG {bs.Player.Energy}", 200, 8, 12, new Color(80, 185, 235, 255));
                        bool disHovFP = Raylib.CheckCollisionPointRec(mouse, new Rectangle(197, 22, 80, 16));
                        Raylib.DrawText($"DIS {bs.BurnPile.Count}", 200, 26, 11, disHovFP ? Color.White : new Color(205, 140, 48, 255));
                        if (!isPaused && disHovFP && Raylib.IsMouseButtonPressed(MouseButton.Left))
                            { showBurnPile = !showBurnPile; dragging = false; draggingIndex = -1; }
                        bool isPlayerTurnFP = bs.Phase == TurnPhase.PlayerTurn;
                        string phaseLblFP = isPlayerTurnFP ? "YOUR TURN" : "ENEMY TURN";
                        Color phaseColFP = isPlayerTurnFP ? new Color(88, 200, 110, 255) : new Color(210, 80, 68, 255);
                        int phWFP = Raylib.MeasureText(phaseLblFP, 13);
                        Raylib.DrawText(phaseLblFP, width / 2 - phWFP / 2, 18, 13, phaseColFP);
                        if (isPlayerTurnFP && !isPaused && DrawButton(new Rectangle(width - 178, 7, 168, 40), "END TURN", new Color(55, 36, 18, 255), new Color(88, 60, 28, 255)))
                        {
                            battleService.EndTurn(); Raylib.PlaySound(ui1Sound);
                            if (bs.Phase != TurnPhase.Finished) { turnBannerText = "YOUR TURN"; turnBannerTimer = 1.4f; }
                        }

                        // Enemy panel (58 to height*0.20)
                        int epBot = (int)(height * 0.20f);
                        Raylib.DrawRectangleRounded(new Rectangle(width / 2 - 280, 58, 560, epBot - 64), 0.08f, 6, fpPanel);
                        Raylib.DrawRectangleRoundedLinesEx(new Rectangle(width / 2 - 280, 58, 560, epBot - 64), 0.08f, 6, 1.5f, fpBord);
                        string eName = activeBattleNode?.Name ?? "Enemy";
                        int eNW = Raylib.MeasureText(eName, 15);
                        Raylib.DrawText(eName, width / 2 - eNW / 2, 66, 15, new Color(200, 85, 75, 255));
                        DrawBar(width / 2 - 180, 88, 360, 9, (float)bs.Enemy.Hp / Math.Max(bs.Enemy.MaxHp, 1), new Color(80, 20, 20, 255), new Color(28, 22, 18, 255));
                        DrawBar(width / 2 - 180, 88, 360, 9, Math.Max(0f, displayedEnemyHp) / Math.Max(bs.Enemy.MaxHp, 1), new Color(200, 55, 55, 255), Color.Blank);
                        int eHpW = Raylib.MeasureText($"{bs.Enemy.Hp} / {bs.Enemy.MaxHp}", 11);
                        Raylib.DrawText($"{bs.Enemy.Hp} / {bs.Enemy.MaxHp}", width / 2 - eHpW / 2, 101, 11, fpText);
                        var (intentStr, intentDmg, intentLane) = TurnManager.GetEnemyIntent(bs);
                        int intentW = Raylib.MeasureText(intentStr, 12);
                        bool intentIsAttack = intentStr.StartsWith("ATTACK");
                        Raylib.DrawText(intentStr, width / 2 - intentW / 2, epBot - 36, 12,
                            intentIsAttack ? new Color(215, 80, 65, 255) : new Color(225, 185, 50, 255));
                        int eFire  = bs.Enemy.ActiveElements.GetStacks(ElementType.Fire);
                        int eFrost = bs.Enemy.ActiveElements.GetStacks(ElementType.Frost);
                        int eBio   = bs.Enemy.ActiveElements.GetStacks(ElementType.Bio);
                        if (eFire  > 0) Raylib.DrawText($"Fire {eFire}",   width / 2 - 160, epBot - 18, 11, new Color(225, 115, 38, 255));
                        if (eFrost > 0) Raylib.DrawText($"Frost {eFrost}", width / 2 + 20,  epBot - 18, 11, new Color(88, 185, 235, 255));
                        if (eBio   > 0) Raylib.DrawText($"Bio {eBio}",     width / 2 + 100, epBot - 18, 11, new Color(68, 200, 80, 255));
                        // Status tooltips
                        bool fpFireHov  = eFire  > 0 && Raylib.CheckCollisionPointRec(mouse, new Rectangle(width / 2 - 164, epBot - 22, 70, 18));
                        bool fpFrostHov = eFrost > 0 && Raylib.CheckCollisionPointRec(mouse, new Rectangle(width / 2 + 16,  epBot - 22, 70, 18));
                        bool fpBioHov   = eBio   > 0 && Raylib.CheckCollisionPointRec(mouse, new Rectangle(width / 2 + 96,  epBot - 22, 60, 18));
                        if (fpFireHov || fpFrostHov || fpBioHov)
                        {
                            string fpTtTitle = fpFireHov  ? $"Fire  x{eFire}"  : fpFrostHov ? $"Frost  x{eFrost}"  : $"Bio  x{eBio}";
                            string fpTtBody  = fpFireHov  ? "Burns 2 dmg/stack each turn, -1 stack/turn"
                                             : fpFrostHov ? "Reduces enemy attack damage, -1 stack/turn"
                                             :               "Poisons 1 dmg/stack each turn, -1 stack/turn";
                            Color fpTtAccent = fpFireHov  ? new Color(225, 115, 38, 220)
                                             : fpFrostHov ? new Color(88, 185, 235, 220)
                                             :               new Color(68, 200, 80, 220);
                            int fpTtW = 240, fpTtH = 64;
                            float fpTtX = width / 2f - fpTtW / 2f;
                            float fpTtY = epBot - 22 - fpTtH - 6;
                            Raylib.DrawRectangleRounded(new Rectangle(fpTtX, fpTtY, fpTtW, fpTtH), 0.12f, 6, new Color(12, 9, 5, 248));
                            Raylib.DrawRectangleRoundedLinesEx(new Rectangle(fpTtX, fpTtY, fpTtW, fpTtH), 0.12f, 6, 1.2f, fpTtAccent);
                            Raylib.DrawText(fpTtTitle, (int)fpTtX + 10, (int)fpTtY + 10, 13, fpTtAccent);
                            Raylib.DrawText(fpTtBody,  (int)fpTtX + 10, (int)fpTtY + 34, 11, new Color(175, 165, 148, 255));
                        }

                        // Bottom strip (always visible)
                        int stripY = height - 58;
                        Raylib.DrawRectangle(0, stripY, width, 58, new Color(14, 9, 4, 245));
                        Raylib.DrawLine(0, stripY, width, stripY, new Color((byte)fpBord.R, (byte)fpBord.G, (byte)fpBord.B, (byte)160));

                        // Left pile: HAND
                        bool lhov = leftHov || dragging;
                        Raylib.DrawRectangleRounded(leftPileR, 0.2f, 6, lhov ? new Color(55, 38, 18, 255) : fpPanel);
                        Raylib.DrawRectangleRoundedLinesEx(leftPileR, 0.2f, 6, 1.5f, lhov ? fpAcct : fpBord);
                        int lpiW = Raylib.MeasureText($"HAND ×{bs.Hand.Count}", 13);
                        Raylib.DrawText($"HAND ×{bs.Hand.Count}", (int)(leftPileR.X + (leftPileR.Width - lpiW) / 2), (int)(leftPileR.Y + 17), 13, lhov ? fpAcct : fpText);

                        // Right pile: FIELD
                        int occFP = bs.PlayerBoard.Count(s => s.IsOccupied);
                        bool rhov2 = rightHov && !dragging;
                        Raylib.DrawRectangleRounded(rightPileR, 0.2f, 6, rhov2 ? new Color(28, 42, 55, 255) : fpPanel);
                        Raylib.DrawRectangleRoundedLinesEx(rightPileR, 0.2f, 6, 1.5f, rhov2 ? Color.SkyBlue : fpBord);
                        int rpiW = Raylib.MeasureText($"FIELD ×{occFP}", 13);
                        Raylib.DrawText($"FIELD ×{occFP}", (int)(rightPileR.X + (rightPileR.Width - rpiW) / 2), (int)(rightPileR.Y + 17), 13, rhov2 ? Color.SkyBlue : fpText);

                        // Ability bar in strip center
                        if (bs.EquippedAbility != null)
                        {
                            float abX = leftPileR.X + leftPileR.Width + 8;
                            float abW = rightPileR.X - abX - 8;
                            if (DrawAbilityBar(abX, stripY + 6, abW, 46, bs, isPlayerTurnFP))
                                if (battleService.ActivateAbility()) Raylib.PlaySound(whoosh7);
                        }

                        // 2.5D layered dungeon battlefield
                        if (showGrid)
                        {
                            int bL3 = 0, bR3 = width;
                            int bTop3 = epBot + 4;
                            int bBot3 = showHandCards ? stripY - 134 : stripY - 4;
                            float bH3 = bBot3 - bTop3;

                            // Layer 1: abyss ceiling — deep black void
                            Raylib.DrawRectangleGradientV(bL3, bTop3, bR3, (int)(bH3 * 0.30f),
                                new Color(0, 0, 0, 255), new Color(10, 6, 3, 255));
                            // Layer 2: rough-cut back wall — cold dark stone
                            Raylib.DrawRectangleGradientV(bL3, bTop3 + (int)(bH3 * 0.30f), bR3, (int)(bH3 * 0.22f),
                                new Color(10, 6, 3, 255), new Color(22, 14, 8, 255));
                            // Layer 3: dungeon floor — warm worn stone
                            Raylib.DrawRectangleGradientV(bL3, bTop3 + (int)(bH3 * 0.52f), bR3, bBot3 - (bTop3 + (int)(bH3 * 0.52f)),
                                new Color(22, 14, 8, 255), new Color(42, 28, 14, 255));

                            // Thick stone pillars with carved edges
                            for (int ci = 1; ci < 5; ci++)
                            {
                                int pcx = bL3 + (bR3 - bL3) * ci / 5;
                                int pTop = bTop3 + (int)(bH3 * 0.04f);
                                int pH   = (int)(bH3 * 0.50f);
                                // Pillar body
                                Raylib.DrawRectangle(pcx - 14, pTop, 28, pH, new Color(16, 10, 5, 255));
                                // Stone highlight (chiseled left edge)
                                Raylib.DrawLine(pcx - 14, pTop, pcx - 14, pTop + pH, new Color(55, 38, 20, 160));
                                // Shadow (right edge)
                                Raylib.DrawLine(pcx + 14, pTop, pcx + 14, pTop + pH, new Color(4, 2, 1, 255));
                                // Capital and base blocks
                                Raylib.DrawRectangle(pcx - 18, pTop, 36, 8, new Color(20, 13, 7, 255));
                                Raylib.DrawRectangle(pcx - 16, pTop + pH - 6, 32, 8, new Color(20, 13, 7, 255));
                                // Crack detail on pillar
                                Raylib.DrawLine(pcx - 3, pTop + pH / 4, pcx + 5, pTop + pH / 3, new Color(6, 3, 1, 200));
                                Raylib.DrawLine(pcx - 3, pTop + pH / 3, pcx,     pTop + pH / 2, new Color(6, 3, 1, 200));
                            }
                            // Wall crack details (background atmosphere)
                            Raylib.DrawLine(bR3 / 5,     bTop3 + (int)(bH3 * 0.12f), bR3 / 5 + 8,  bTop3 + (int)(bH3 * 0.22f), new Color(5, 3, 1, 180));
                            Raylib.DrawLine(bR3 * 3 / 5, bTop3 + (int)(bH3 * 0.08f), bR3 * 3 / 5 - 6, bTop3 + (int)(bH3 * 0.20f), new Color(5, 3, 1, 180));

                            // Torch glow — wide warm bloom + bright core
                            for (int ci = 1; ci < 5; ci++)
                            {
                                float tcx = bL3 + (bR3 - bL3) * ci / 5f;
                                int tcy3 = bTop3 + (int)(bH3 * 0.32f);
                                // Wide ambient bloom
                                Raylib.DrawCircleV(new Vector2(tcx, tcy3), 75, new Color(180, 95, 10, 8));
                                Raylib.DrawCircleV(new Vector2(tcx, tcy3), 52, new Color(210, 120, 20, 14));
                                // Mid glow
                                Raylib.DrawCircleV(new Vector2(tcx, tcy3), 32, new Color(230, 150, 40, 22));
                                // Bright core
                                Raylib.DrawCircleV(new Vector2(tcx, tcy3), 14, new Color(255, 200, 80, 40));
                                Raylib.DrawCircleV(new Vector2(tcx, tcy3), 6,  new Color(255, 240, 160, 80));
                                // Torch bracket (small rectangle on pillar)
                                Raylib.DrawRectangle((int)tcx - 4, tcy3 + 8, 8, 10, new Color(55, 35, 12, 255));
                            }
                            // Heavy ceiling shadow — dungeon oppression
                            Raylib.DrawRectangleGradientV(bL3, bTop3, bR3, 32, new Color(0, 0, 0, 255), new Color(0, 0, 0, 0));
                            // Side vignettes
                            Raylib.DrawRectangleGradientH(bL3, bTop3, 60, bBot3 - bTop3, new Color(0, 0, 0, 180), new Color(0, 0, 0, 0));
                            Raylib.DrawRectangleGradientH(bR3 - 60, bTop3, 60, bBot3 - bTop3, new Color(0, 0, 0, 0), new Color(0, 0, 0, 180));

                            // 5 lane rows: far (top/thin/dim) → near (bottom/thick/bright)
                            float lanesTop3 = bTop3 + bH3 * 0.48f;
                            float lanesEnd3 = bBot3 - 1f;
                            float lanesH3   = lanesEnd3 - lanesTop3;
                            float[] lProps   = { 0.14f, 0.16f, 0.18f, 0.24f, 0.28f };
                            float[] laneY    = new float[6];
                            laneY[0] = lanesTop3;
                            for (int li = 0; li < 5; li++) laneY[li + 1] = laneY[li] + lanesH3 * lProps[li];

                            Color[] laneFill3 = {
                                new Color(12, 7, 3, 255), new Color(16, 10, 5, 255),
                                new Color(20, 13, 7, 255), new Color(26, 16, 8, 255), new Color(34, 22, 10, 255)
                            };
                            float[] scaleL = { 0.50f, 0.62f, 0.74f, 0.87f, 1.00f };
                            float splitX3 = width * 0.38f;

                            // Draw lane rows (back to front)
                            for (int li = 0; li < 5; li++)
                            {
                                float lT3 = laneY[li], lB3 = laneY[li + 1];
                                Raylib.DrawRectangle(bL3, (int)lT3, bR3, (int)(lB3 - lT3) + 1, laneFill3[li]);
                                Raylib.DrawLine(bL3, (int)lT3, bR3, (int)lT3, new Color(60, 40, 20, li == 0 ? 200 : 130));
                                // Stone tile grout lines
                                for (float fy3 = lT3 + 8; fy3 < lB3 - 3; fy3 += 14)
                                    Raylib.DrawLine(bL3, (int)fy3, (int)splitX3, (int)fy3, new Color(8, 5, 2, 60));
                                // Random crack per lane (deterministic via li)
                                int crx1 = bL3 + (li * 97 % (int)(splitX3 * 0.7f));
                                int cry1 = (int)(lT3 + (lB3 - lT3) * 0.3f);
                                Raylib.DrawLine(crx1, cry1, crx1 + 12 + li * 5, cry1 + (int)(lB3 - lT3) / 2, new Color(4, 2, 1, 120));
                                // Lane number (subtle)
                                Raylib.DrawText($"L{li}", 5, (int)(lT3 + (lB3 - lT3) / 2 - 6), 10, new Color(40, 28, 14, 100));
                            }
                            Raylib.DrawLine(bL3, (int)lanesEnd3, bR3, (int)lanesEnd3, new Color(60, 40, 20, 200));

                            // Zone labels above lanes
                            int dlW3 = Raylib.MeasureText("DEPLOY", 10);
                            Raylib.DrawText("DEPLOY", (int)(splitX3 / 2 - dlW3 / 2), (int)(lanesTop3 - 16), 10, new Color(95, 72, 38, 155));
                            int elW3 = Raylib.MeasureText("ENEMY ZONE", 10);
                            Raylib.DrawText("ENEMY ZONE", (int)(splitX3 + (bR3 - splitX3) / 2 - elW3 / 2), (int)(lanesTop3 - 16), 10, new Color(115, 46, 46, 155));

                            if (executeMode)
                            {
                                int xhW3 = Raylib.MeasureText("CLICK UNIT TO EXECUTE  (costs 1 energy)", 12);
                                Raylib.DrawText("CLICK UNIT TO EXECUTE  (costs 1 energy)", width / 2 - xhW3 / 2, (int)(lanesTop3 - 16), 12, new Color(80, 185, 235, 220));
                            }

                            // Lane contents (units + enemy)
                            for (int li = 0; li < 5; li++)
                            {
                                float lT3 = laneY[li], lB3 = laneY[li + 1], lHx = lB3 - lT3;
                                float sc3 = scaleL[li];

                                Rectangle deployHitFP = new Rectangle(bL3 + 2, lT3 + 2, splitX3 - 6, lHx - 4);
                                bool isDeployTgt = showHandCards && dragging && Raylib.CheckCollisionPointRec(mouse, deployHitFP);
                                if (isDeployTgt)
                                    Raylib.DrawRectangleRec(deployHitFP, new Color(188, 148, 65, 28));
                                if (isDeployTgt)
                                    Raylib.DrawRectangleLinesEx(deployHitFP, 2.5f, fpAcct);

                                if (bs.PlayerBoard[li].IsOccupied)
                                {
                                    var occ3 = bs.PlayerBoard[li].Occupant!;
                                    bool uSel3 = bs.SelectedBoardSlot == li;
                                    float uW3 = 90f * sc3, uH3 = 110f * sc3;
                                    float uCX3 = splitX3 * 0.48f;
                                    float uCY3 = lT3 + lHx * 0.50f;
                                    DrawSummonedUnit(new Rectangle(uCX3 - uW3 / 2, uCY3 - uH3 / 2, uW3, uH3),
                                        occ3.SourceCard.Name, occ3.Hp, occ3.MaxHp, uSel3);
                                    int atkFS = Math.Max(8, (int)(9 * sc3));
                                    int atkWx = Raylib.MeasureText($"ATK {occ3.BaseAttack + bs.AbilityUnitAttackBuff}", atkFS);
                                    Raylib.DrawText($"ATK {occ3.BaseAttack + bs.AbilityUnitAttackBuff}",
                                        (int)(uCX3 - atkWx / 2), (int)(uCY3 - uH3 / 2 - 13 * sc3), atkFS,
                                        uSel3 ? fpAcct : new Color(140, 118, 68, 200));
                                    if (!isPaused && executeMode)
                                    {
                                        if (Raylib.CheckCollisionPointRec(mouse, deployHitFP))
                                            Raylib.DrawRectangleRec(deployHitFP, new Color(80, 185, 235, 22));
                                        if (Raylib.CheckCollisionPointRec(mouse, deployHitFP) && Raylib.IsMouseButtonPressed(MouseButton.Left))
                                            battleService.ExecuteCard(li);
                                    }
                                    else if (!isPaused && Raylib.IsMouseButtonPressed(MouseButton.Left) && Raylib.CheckCollisionPointRec(mouse, deployHitFP))
                                        bs.SelectedBoardSlot = bs.SelectedBoardSlot == li ? -1 : li;
                                }
                                else if (!isDeployTgt)
                                {
                                    int emW3 = Raylib.MeasureText("—", (int)(11 * sc3));
                                    Raylib.DrawText("—", (int)(splitX3 / 2 - emW3 / 2), (int)(lT3 + lHx / 2 - 6 * sc3), (int)(11 * sc3), new Color(55, 44, 26, 110));
                                }
                                // Death flash on lane deploy zone
                                if (slotFlashTimers[li] > 0)
                                {
                                    byte ffp = (byte)Math.Min(200, (int)(slotFlashTimers[li] / 0.3f * 200));
                                    Raylib.DrawRectangleRec(deployHitFP, new Color((byte)255, (byte)255, (byte)255, ffp));
                                }

                                // Enemy circle
                                if (bs.Enemy.Position == li)
                                {
                                    float erCX3 = splitX3 + (bR3 - splitX3) * 0.42f;
                                    float erCY3 = lT3 + lHx * 0.50f;
                                    float er3   = Math.Min(lHx * 0.42f, 62f * sc3);
                                    if (bs.Enemy.ActiveElements.GetStacks(ElementType.Fire) > 0)
                                        Raylib.DrawCircleLines((int)erCX3, (int)erCY3, (int)(er3 + 10), new Color(210, 85, 18, 90));
                                    if (bs.Enemy.ActiveElements.GetStacks(ElementType.Frost) > 0)
                                        Raylib.DrawCircleLines((int)erCX3, (int)erCY3, (int)(er3 + 14), new Color(80, 175, 230, 70));
                                    Raylib.DrawCircleV(new Vector2(erCX3, erCY3), er3, new Color(100, 25, 25, 255));
                                    Raylib.DrawCircleLines((int)erCX3, (int)erCY3, (int)er3, new Color(190, 60, 60, 255));
                                    Raylib.DrawCircleV(new Vector2(erCX3 - er3 * 0.28f, erCY3 - er3 * 0.14f), er3 * 0.13f, new Color(240, 200, 80, 255));
                                    Raylib.DrawCircleV(new Vector2(erCX3 + er3 * 0.28f, erCY3 - er3 * 0.14f), er3 * 0.13f, new Color(240, 200, 80, 255));
                                    Raylib.DrawLineEx(new Vector2(erCX3 - er3 * 0.26f, erCY3 + er3 * 0.25f), new Vector2(erCX3 + er3 * 0.26f, erCY3 + er3 * 0.25f), 2, new Color(240, 200, 80, 180));
                                    // Hit flash overlay
                                    if (enemyFlashTimer > 0)
                                    {
                                        byte fa = (byte)Math.Min(200, (int)(enemyFlashTimer * 5 * 255));
                                        Raylib.DrawCircleV(new Vector2(erCX3, erCY3), er3, new Color((byte)255, (byte)255, (byte)255, fa));
                                    }
                                    enemyScreenPos = new Vector2(erCX3, erCY3);
                                    int ehpFPx = Raylib.MeasureText($"{bs.Enemy.Hp}", (int)Math.Max(10, 12 * sc3));
                                    Raylib.DrawText($"{bs.Enemy.Hp}", (int)(erCX3 - ehpFPx / 2), (int)(erCY3 + er3 + 2), (int)Math.Max(10, 12 * sc3), new Color(200, 180, 120, 255));
                                }
                            }

                            // Drop release onto lane
                            if (!isPaused && !Raylib.IsMouseButtonDown(MouseButton.Left) && dragging && !isDraggingFromCollectionPool)
                            {
                                for (int li = 0; li < 5; li++)
                                {
                                    Rectangle dropFP = new Rectangle(bL3 + 2, laneY[li] + 2, splitX3 - 6, (laneY[li + 1] - laneY[li]) - 4);
                                    if (Raylib.CheckCollisionPointRec(mouse, dropFP)) { if (battleService.PlaceCard(draggingIndex, li)) Raylib.PlaySound(whooshSounds[sfxRng.Next(5)]); break; }
                                }
                                dragging = false; draggingIndex = -1;
                            }

                            // Hand cards when left pile is hovered
                            if (showHandCards)
                            {
                                int hcY3 = bBot3 + 4;
                                int hcW3 = 88, hcH3 = 120, hcSp3 = 94;
                                int hovH3 = -1;
                                for (int i = 0; i < bs.Hand.Count; i++)
                                {
                                    if (dragging && draggingIndex == i) continue;
                                    if (Raylib.CheckCollisionPointRec(mouse, new Rectangle(12 + i * hcSp3, hcY3, hcW3, hcH3))) hovH3 = i;
                                }
                                for (int i = 0; i < bs.Hand.Count; i++)
                                {
                                    if (dragging && draggingIndex == i) continue;
                                    float lift3 = hovH3 == i ? -18f : 0f;
                                    Rectangle hR3 = new Rectangle(12 + i * hcSp3, hcY3 + lift3, hcW3, hcH3);
                                    var hC3 = bs.Hand[i];
                                    DrawCard(hR3, hC3.Name, hC3.Description, hC3.Cost, new Color(28, 20, 10, 255), hovH3 == i ? fpAcct : fpBord);
                                    if (!isPaused && Raylib.IsMouseButtonPressed(MouseButton.Left) && Raylib.CheckCollisionPointRec(mouse, new Rectangle(12 + i * hcSp3, hcY3, hcW3, hcH3)) && !dragging)
                                    {
                                        dragging = true; draggingIndex = i; isDraggingFromCollectionPool = false;
                                        dragOffset = mouse - new Vector2(hR3.X, hR3.Y);
                                    }
                                }
                                if (hovH3 >= 0 && !dragging && hovH3 < bs.Hand.Count)
                                {
                                    int pvW3 = 145, pvH3b = 200;
                                    int pvX3 = (int)Math.Clamp(12 + hovH3 * hcSp3 + hcW3 / 2 - pvW3 / 2, 4, width - pvW3 - 4);
                                    DrawCard(new Rectangle(pvX3, hcY3 - pvH3b - 6, pvW3, pvH3b),
                                        bs.Hand[hovH3].Name, bs.Hand[hovH3].Description, bs.Hand[hovH3].Cost,
                                        new Color(28, 20, 10, 255), fpAcct);
                                }
                            }

                        }

                        // Fantasy drag ghost
                        if (dragging && draggingIndex >= 0 && draggingIndex < bs.Hand.Count)
                        {
                            var dc = bs.Hand[draggingIndex];
                            DrawCard(new Rectangle(mouse.X - dragOffset.X, mouse.Y - dragOffset.Y, 120, 165), dc.Name, dc.Description, dc.Cost, new Color(44, 32, 16, 230), fpAcct);
                        }
                    }

                    // Floating damage numbers
                    foreach (var fd in floatDmgs)
                    {
                        byte alpha = (byte)Math.Max(0, Math.Min(255, (int)(fd.Life * 255)));
                        Color fc2 = new Color(fd.Col.R, fd.Col.G, fd.Col.B, alpha);
                        string dmgStr = fd.Amount > 0 ? $"-{fd.Amount}" : "0";
                        int fnW = Raylib.MeasureText(dmgStr, 20);
                        Raylib.DrawText(dmgStr, (int)(fd.X - fnW / 2), (int)fd.Y, 20, fc2);
                    }

                    // Turn banner (both themes)
                    if (turnBannerTimer > 0 && bs.Phase != TurnPhase.Finished)
                    {
                        float bnAlpha = Math.Min(1f, turnBannerTimer * 2.5f);
                        byte bnA = (byte)(bnAlpha * 230);
                        bool isYT = turnBannerText == "YOUR TURN";
                        Color bnCol = isYT ? new Color((byte)72, (byte)220, (byte)95, bnA) : new Color((byte)220, (byte)62, (byte)62, bnA);
                        byte bnBgA = (byte)(bnA / 3);
                        Raylib.DrawRectangle(0, height / 2 - 46, width, 80, new Color((byte)0, (byte)0, (byte)0, bnBgA));
                        int bnW = Raylib.MeasureText(turnBannerText, 52);
                        Raylib.DrawText(turnBannerText, width / 2 - bnW / 2, height / 2 - 30, 52, bnCol);
                        Raylib.DrawLineEx(new Vector2(width / 2 - bnW / 2f, height / 2 + 24f), new Vector2(width / 2 + bnW / 2f, height / 2 + 24f), 2f, bnCol);
                    }

                    // Keybinding hint bar
                    if (bs.Phase == TurnPhase.PlayerTurn && !isPaused && !showBurnPile)
                    {
                        string hint = "ENTER  end turn    Q  ability    P  pause    SPACE  execute";
                        int hintPx = Raylib.MeasureText(hint, 11);
                        Raylib.DrawRectangle(width / 2 - hintPx / 2 - 10, height - 22, hintPx + 20, 18, new Color(0, 0, 0, 130));
                        Raylib.DrawText(hint, width / 2 - hintPx / 2, height - 20, 11, new Color(90, 95, 110, 200));
                    }

                    // Win/Lose overlay (both themes)
                    if (bs.Phase == TurnPhase.Finished)
                    {
                        if (!battleEndSoundPlayed)
                        {
                            battleEndSoundPlayed = true;
                            Raylib.PlaySound(bs.Enemy.IsDead ? winSound : loseSound);
                        }
                        Raylib.DrawRectangle(0, 0, width, height, new Color(8, 9, 13, 215));
                        if (bs.Enemy.IsDead)
                        {
                            int goldReward = activeBattleNode?.Type switch { NodeType.CombatElite => 80, NodeType.CombatBoss => 120, _ => 60 };
                            Raylib.DrawRectangle(width / 2 - 220, height / 2 - 80, 440, 205, new Color(18, 22, 16, 240));
                            Raylib.DrawRectangleLinesEx(new Rectangle(width / 2 - 220, height / 2 - 80, 440, 205), 1.5f, new Color(72, 185, 88, 160));
                            int vcw = Raylib.MeasureText("VICTORY", 56);
                            Raylib.DrawText("VICTORY", width / 2 - vcw / 2, height / 2 - 72, 56, Color.Gold);
                            Raylib.DrawLineEx(new Vector2(width / 2 - 180f, height / 2 - 8f), new Vector2(width / 2 + 180f, height / 2 - 8f), 1f, new Color(68, 155, 78, 160));
                            string goldLine = $"+{goldReward} Credits  —  choose a card reward";
                            int r1w2 = Raylib.MeasureText(goldLine, 15);
                            Raylib.DrawText(goldLine, width / 2 - r1w2 / 2, height / 2 + 4, 15, new Color(200, 185, 80, 255));
                            string winStats = $"Turns: {bs.EnemyTurnCount}   Cards played: {bs.CardsPlayedTotal}   HP left: {bs.Player.Hp}/{bs.Player.MaxHp}";
                            int winStatsW = Raylib.MeasureText(winStats, 12);
                            Raylib.DrawText(winStats, width / 2 - winStatsW / 2, height / 2 + 26, 12, new Color(120, 148, 120, 255));
                            bool isBossVictory = activeBattleNode?.Type == NodeType.CombatBoss;
                            string rewardBtnLabel = isBossVictory ? "COMPLETE RUN →" : "CHOOSE REWARD →";
                            if (DrawButton(new Rectangle(width / 2 - 130, height / 2 + 52, 260, 44), rewardBtnLabel, new Color(38, 72, 42, 255), new Color(55, 108, 62, 255)))
                            {
                                profile.Gold += goldReward;
                                profile.PlayerHp = bs.Player.Hp;
                                if (activeBattleNode != null)
                                {
                                    activeBattleNode.Completed = true;
                                    completedNodes.Add(activeBattleNode.Id);
                                }
                                if (isBossVictory)
                                {
                                    SaveGame(profile, completedNodes);
                                    scene = GameScene.CampaignVictory;
                                }
                                else
                                {
                                    // Generate 3 distinct random card options
                                    var pool = CardLibrary.GetAll().ToList();
                                    for (int ri = pool.Count - 1; ri > 0; ri--)
                                    {
                                        int rj = rewardRng.Next(ri + 1);
                                        (pool[ri], pool[rj]) = (pool[rj], pool[ri]);
                                    }
                                    rewardOptions = pool.Take(3).ToList();
                                    scene = GameScene.RewardChoice;
                                }
                            }
                        }
                        else
                        {
                            Raylib.DrawRectangle(width / 2 - 270, height / 2 - 80, 540, 245, new Color(22, 14, 14, 240));
                            Raylib.DrawRectangleLinesEx(new Rectangle(width / 2 - 270, height / 2 - 80, 540, 245), 1.5f, new Color(185, 52, 52, 160));
                            int dfw = Raylib.MeasureText("DEFEAT", 56);
                            Raylib.DrawText("DEFEAT", width / 2 - dfw / 2, height / 2 - 68, 56, new Color(215, 60, 60, 255));
                            Raylib.DrawLineEx(new Vector2(width / 2 - 180f, height / 2 - 4f), new Vector2(width / 2 + 180f, height / 2 - 4f), 1f, new Color(155, 48, 48, 160));
                            string loseStats = $"Turns: {bs.EnemyTurnCount}   Cards played: {bs.CardsPlayedTotal}   Dmg dealt: {bs.Enemy.MaxHp - bs.Enemy.Hp}";
                            int loseStatsW = Raylib.MeasureText(loseStats, 12);
                            Raylib.DrawText(loseStats, width / 2 - loseStatsW / 2, height / 2 + 12, 12, new Color(148, 115, 115, 255));
                            if (!confirmRestart)
                            {
                                if (DrawButton(new Rectangle(width / 2 - 115, height / 2 + 36, 230, 44), "RESTART RUN", new Color(88, 28, 28, 255), new Color(128, 42, 42, 255)))
                                    confirmRestart = true;
                            }
                            else
                            {
                                int cfW = Raylib.MeasureText("All progress will be lost. Are you sure?", 13);
                                Raylib.DrawText("All progress will be lost. Are you sure?", width / 2 - cfW / 2, height / 2 + 38, 13, new Color(210, 155, 155, 255));
                                if (DrawButton(new Rectangle(width / 2 - 126, height / 2 + 62, 118, 40), "YES, RESTART", new Color(100, 28, 28, 255), new Color(148, 42, 42, 255)))
                                {
                                    profile.Gold = 150;
                                    profile.SkillPoints = 8;
                                    profile.PlayerHp = 0;
                                    completedNodes.Clear();
                                    foreach (var cn in campaignNodes) cn.Completed = false;
                                    SaveGame(profile, completedNodes);
                                    confirmRestart = false;
                                    scene = GameScene.TitleScreen;
                                }
                                if (DrawButton(new Rectangle(width / 2 + 8, height / 2 + 62, 118, 40), "CANCEL", new Color(32, 36, 48, 255), new Color(48, 55, 72, 255)))
                                    confirmRestart = false;
                            }
                        }
                    }
                    break;

                case GameScene.MarketShop:
                    // Header
                    Raylib.DrawRectangle(0, 0, width, 58, new Color(15, 17, 22, 255));
                    Raylib.DrawLine(0, 58, width, 58, new Color(38, 43, 52, 255));
                    Raylib.DrawText("MARKET  &  SKILL MATRIX", 30, 15, 22, Color.White);
                    Raylib.DrawText($"{profile.Gold} G", width - 215, 15, 20, Color.Gold);
                    Raylib.DrawText($"{profile.SkillPoints} SP", width - 100, 15, 20, Color.SkyBlue);

                    if (DrawButton(new Rectangle(30, 74, 160, 38), "< BACK TO MAP", new Color(35, 38, 48, 255), new Color(52, 56, 70, 255)))
                        scene = GameScene.CampaignMap;

                    // Vertical divider
                    Raylib.DrawLine(width / 2, 60, width / 2, height, new Color(35, 40, 50, 255));

                    // === LEFT HALF: SHOP  (4 per row, 2 rows max) ===
                    Raylib.DrawText("SCHEMA CARDS", 30, 132, 18, Color.Gold);
                    Raylib.DrawLine(30, 156, width / 2 - 20, 156, new Color(90, 75, 25, 110));
                    {
                        int shopCols = 4;
                        int shopCardW = (width / 2 - 60) / shopCols - 12;
                        int shopCardH = 220;
                        for (int i = 0; i < shopInventory.Count; i++)
                        {
                            int sRow = i / shopCols, sCol = i % shopCols;
                            int sx2 = 30 + sCol * (shopCardW + 12);
                            int sy2 = 168 + sRow * (shopCardH + 54);
                            bool alreadyOwned = profile.TotalCollection.Any(c => c.Id == shopInventory[i].Id);
                            DrawCard(new Rectangle(sx2, sy2, shopCardW, shopCardH), shopInventory[i].Name, shopInventory[i].Description, shopInventory[i].Cost, new Color(20, 24, 34, 255), alreadyOwned ? new Color(55, 88, 55, 255) : new Color(40, 62, 108, 255));
                            if (alreadyOwned)
                            {
                                int ownW = Raylib.MeasureText("OWNED", 11);
                                Raylib.DrawRectangle(sx2 + shopCardW / 2 - ownW / 2 - 6, sy2 + shopCardH - 26, ownW + 12, 20, new Color(38, 88, 48, 230));
                                Raylib.DrawText("OWNED", sx2 + shopCardW / 2 - ownW / 2, sy2 + shopCardH - 22, 11, new Color(80, 220, 100, 255));
                            }
                            bool canAfford = profile.Gold >= cardShopCost && !alreadyOwned;
                            if (DrawButton(new Rectangle(sx2, sy2 + shopCardH + 4, shopCardW, 36), alreadyOwned ? "OWNED" : $"BUY  {cardShopCost} G",
                                canAfford ? new Color(35, 60, 45, 255) : new Color(42, 36, 36, 255),
                                canAfford ? new Color(50, 95, 65, 255) : new Color(58, 42, 42, 255)) && canAfford)
                            {
                                profile.Gold -= cardShopCost;
                                profile.TotalCollection.Add(shopInventory[i]);
                                Raylib.PlaySound(ui2Sound);
                            }
                        }
                    }

                    // === RIGHT HALF: SKILL TREE ===
                    int treeLeft = width / 2 + 20;
                    float treeSpacing = Math.Min(80f, width * 0.044f);
                    float treeOX = width * 0.765f;
                    float treeOY = height * 0.56f;

                    Raylib.DrawText("SKILL MATRIX", treeLeft + 20, 132, 18, Color.SkyBlue);
                    Raylib.DrawLine(treeLeft + 20, 156, treeLeft + 195, 156, new Color(25, 85, 105, 110));
                    Raylib.DrawText(profile.SkillTree.DebugMode ? "DEBUG  [F3]" : "[F3]",
                        treeLeft + 210, 137, 13,
                        profile.SkillTree.DebugMode ? new Color(200, 80, 80, 255) : new Color(52, 60, 72, 255));

                    // Connectors drawn first (underneath nodes)
                    foreach (var sn in profile.SkillTree.Nodes)
                    {
                        Vector2 snPos = GetSkillNodePos(sn, treeOX, treeOY, treeSpacing);
                        foreach (var reqId in sn.PrerequisiteIds)
                        {
                            var req = profile.SkillTree.Nodes.Find(n => n.Id == reqId);
                            if (req == null) continue;
                            Vector2 reqPos = GetSkillNodePos(req, treeOX, treeOY, treeSpacing);
                            Color lc = profile.SkillTree.DebugMode
                                ? (sn.IsUnlocked ? GetBranchColor(sn.Branch) : new Color(40, 42, 48, 255))
                                : (sn.IsUnlocked ? new Color(0, 215, 235, 210) : new Color(32, 125, 145, 50));
                            Raylib.DrawLineEx(reqPos, snPos, sn.IsUnlocked ? 3f : 2f, lc);
                        }
                    }

                    SkillNode? hoveredNode = null;

                    foreach (var sn in profile.SkillTree.Nodes)
                    {
                        Vector2 snPos = GetSkillNodePos(sn, treeOX, treeOY, treeSpacing);
                        float nr = sn.Branch == SkillBranch.Origin ? 24f : 20f;
                        bool avail = profile.SkillTree.IsAvailable(sn.Id);

                        if (Raylib.CheckCollisionPointCircle(mouse, snPos, nr + 8)) hoveredNode = sn;

                        if (profile.SkillTree.DebugMode)
                        {
                            Color dc = GetBranchColor(sn.Branch);
                            Raylib.DrawCircleV(snPos, nr, sn.IsUnlocked ? dc : new Color(15, 17, 23, 255));
                            Raylib.DrawCircleLines((int)snPos.X, (int)snPos.Y, nr,
                                sn.IsUnlocked ? Color.White : (avail ? dc : new Color(46, 48, 56, 255)));
                            Raylib.DrawText($"{sn.Id}", (int)snPos.X - 5, (int)snPos.Y - 7, 13,
                                sn.IsUnlocked ? Color.Black : Color.DarkGray);
                            Raylib.DrawText(sn.Branch.ToString()[..1], (int)snPos.X - 10, (int)snPos.Y - 33, 13, dc);
                        }
                        else
                        {
                            Color outline = sn.IsUnlocked ? new Color(0, 225, 245, 255)
                                          : avail         ? new Color(0, 200, 220, 200)
                                          :                 new Color(55, 148, 168, 105);
                            Color fill = sn.IsUnlocked ? new Color(0, 165, 192, 255) : new Color(11, 15, 22, 255);

                            if (avail && !sn.IsUnlocked)
                                Raylib.DrawCircleV(snPos, nr + 7, new Color(0, 190, 210, 25));

                            Raylib.DrawCircleV(snPos, nr, fill);
                            Raylib.DrawCircleLines((int)snPos.X, (int)snPos.Y, nr, outline);
                            Raylib.DrawCircleLines((int)snPos.X, (int)snPos.Y, nr - 1f,
                                new Color(outline.R, outline.G, outline.B, (byte)(outline.A / 3)));

                            DrawNodeIcon(snPos, sn.Branch, sn.IsUnlocked ? Color.White : outline, nr * 0.52f);
                        }
                    }

                    // Tooltip
                    if (hoveredNode != null)
                    {
                        Vector2 hnPos = GetSkillNodePos(hoveredNode, treeOX, treeOY, treeSpacing);
                        int ttW = 198, ttH = 108;
                        float ttX = hnPos.X + 30, ttY = hnPos.Y - ttH / 2f;
                        if (ttX + ttW > width - 8) ttX = hnPos.X - ttW - 30;
                        if (ttY < 65) ttY = 65;
                        if (ttY + ttH > height - 8) ttY = height - ttH - 8;

                        Color ttBorder = profile.SkillTree.DebugMode
                            ? GetBranchColor(hoveredNode.Branch)
                            : new Color(0, 200, 220, 210);
                        Raylib.DrawRectangleRounded(new Rectangle(ttX, ttY, ttW, ttH), 0.12f, 6, new Color(11, 14, 21, 248));
                        Raylib.DrawRectangleRoundedLinesEx(new Rectangle(ttX, ttY, ttW, ttH), 0.12f, 6, 1.5f, ttBorder);

                        Raylib.DrawText(hoveredNode.Name, (int)ttX + 10, (int)ttY + 10, 14, Color.White);
                        Raylib.DrawText(hoveredNode.Description, (int)ttX + 10, (int)ttY + 30, 12, new Color(180, 185, 198, 255));

                        if (hoveredNode.IsUnlocked)
                        {
                            Raylib.DrawText("UNLOCKED", (int)ttX + 10, (int)ttY + 80, 14, new Color(0, 210, 90, 255));
                        }
                        else
                        {
                            int pathCost = profile.SkillTree.GetTotalCostToUnlock(hoveredNode.Id);
                            bool canUnlock = profile.SkillPoints >= pathCost;
                            Raylib.DrawText($"Cost: {pathCost} SP", (int)ttX + 10, (int)ttY + 55, 13,
                                canUnlock ? Color.Yellow : new Color(210, 72, 72, 255));
                            Raylib.DrawText(canUnlock ? "Click to unlock path" : $"Need {pathCost - profile.SkillPoints} more SP",
                                (int)ttX + 10, (int)ttY + 80, 12,
                                canUnlock ? new Color(0, 190, 210, 200) : new Color(155, 70, 70, 200));

                            if (canUnlock && Raylib.IsMouseButtonPressed(MouseButton.Left))
                            {
                                int sp = profile.SkillPoints;
                                profile.SkillTree.UnlockPath(hoveredNode.Id, ref sp);
                                profile.SkillPoints = sp;
                                Raylib.PlaySound(ui2Sound);
                            }
                        }
                    }
                    break;

                case GameScene.DeckBuilder:
                {
                    // Header
                    Raylib.DrawRectangle(0, 0, width, 58, new Color(14, 16, 21, 255));
                    Raylib.DrawLine(0, 58, width, 58, new Color(35, 40, 52, 255));
                    Raylib.DrawText("DECK BUILDER", 28, 16, 22, Color.White);
                    int dbCnt = profile.ActiveDeck.Count;
                    Color dbCntCol = dbCnt == 10 ? Color.Gold : (dbCnt > 0 ? Color.Yellow : new Color(130, 135, 155, 255));
                    int dbCntW = Raylib.MeasureText($"{dbCnt}/10", 22);
                    Raylib.DrawText($"{dbCnt}/10", width / 2 - dbCntW / 2, 16, 22, dbCntCol);
                    Raylib.DrawText("Click card = add/remove  •  Max 10  •  1 copy each", width / 2 - 260, 38, 12, new Color(90, 95, 112, 255));
                    if (DrawButton(new Rectangle(width - 220, 9, 200, 40), "< SAVE & EXIT", new Color(38, 72, 42, 255), new Color(55, 108, 62, 255)))
                        scene = GameScene.CampaignMap;

                    // All cards from library (13 unique)
                    var allCards = CardLibrary.GetAll().ToList();
                    int dbCols = 7;
                    int dbMargin = 48;
                    int dbGap = 12;
                    int dbCW = (width - dbMargin * 2 - dbGap * (dbCols - 1)) / dbCols;
                    int dbCH = (int)(dbCW * 1.38f);
                    int dbStartY = 74;

                    for (int i = 0; i < allCards.Count; i++)
                    {
                        int dRow = i / dbCols, dCol = i % dbCols;
                        int dx = dbMargin + dCol * (dbCW + dbGap);
                        int dy = dbStartY + dRow * (dbCH + 14);
                        bool inDeck = profile.ActiveDeck.Any(c => c.Id == allCards[i].Id);
                        Color dbBody = inDeck ? new Color(22, 38, 28, 255) : new Color(18, 20, 28, 255);
                        Color dbBorder = inDeck ? new Color(68, 185, 88, 255) : new Color(48, 52, 70, 255);
                        bool dbHov = Raylib.CheckCollisionPointRec(mouse, new Rectangle(dx, dy, dbCW, dbCH));
                        if (dbHov && !inDeck) dbBody = new Color(26, 30, 42, 255);

                        DrawCard(new Rectangle(dx, dy, dbCW, dbCH), allCards[i].Name, allCards[i].Description, allCards[i].Cost, dbBody, dbBorder, inDeck);

                        // Overlay "IN DECK" badge
                        if (inDeck)
                        {
                            int bdgW = Raylib.MeasureText("IN DECK", 10);
                            Raylib.DrawRectangle(dx + dbCW - bdgW - 8, dy + 2, bdgW + 6, 16, new Color(55, 155, 72, 210));
                            Raylib.DrawText("IN DECK", dx + dbCW - bdgW - 5, dy + 4, 10, Color.White);
                        }
                        else if (dbCnt >= 10)
                        {
                            // Dim unavailable cards when deck is full
                            Raylib.DrawRectangleRec(new Rectangle(dx, dy, dbCW, dbCH), new Color(0, 0, 0, 80));
                        }

                        if (Raylib.IsMouseButtonPressed(MouseButton.Left) && dbHov)
                        {
                            if (inDeck)
                                { profile.ActiveDeck.RemoveAll(c => c.Id == allCards[i].Id); Raylib.PlaySound(ui2Sound); }
                            else if (dbCnt < 10)
                                { profile.ActiveDeck.Add(allCards[i]); Raylib.PlaySound(ui2Sound); }
                        }
                    }

                    // Current deck row (compact, below the card grid)
                    int deckRowY = dbStartY + ((allCards.Count - 1) / dbCols + 1) * (dbCH + 14) + 12;
                    Raylib.DrawLine(dbMargin, deckRowY, width - dbMargin, deckRowY, new Color(35, 40, 52, 255));
                    deckRowY += 8;
                    Raylib.DrawText("DECK:", dbMargin, deckRowY + 4, 13, new Color(195, 165, 52, 255));
                    for (int i = 0; i < profile.ActiveDeck.Count; i++)
                    {
                        int chipX = dbMargin + 55 + i * 130;
                        int chipY = deckRowY;
                        if (chipX + 120 > width - dbMargin) break;
                        bool chipHov = Raylib.CheckCollisionPointRec(mouse, new Rectangle(chipX, chipY, 120, 28));
                        Raylib.DrawRectangleRounded(new Rectangle(chipX, chipY, 120, 28), 0.3f, 4, chipHov ? new Color(88, 38, 38, 255) : new Color(28, 42, 32, 255));
                        Raylib.DrawRectangleRoundedLinesEx(new Rectangle(chipX, chipY, 120, 28), 0.3f, 4, 1.2f, chipHov ? new Color(188, 68, 68, 200) : new Color(58, 110, 68, 200));
                        int chipTW = Raylib.MeasureText(profile.ActiveDeck[i].Name, 11);
                        Raylib.DrawText(profile.ActiveDeck[i].Name, chipX + 60 - chipTW / 2, chipY + 8, 11, chipHov ? new Color(215, 155, 155, 255) : Color.White);
                        if (chipHov && Raylib.IsMouseButtonPressed(MouseButton.Left))
                        {
                            string rmId = profile.ActiveDeck[i].Id;
                            profile.ActiveDeck.RemoveAll(c => c.Id == rmId);
                        }
                    }

                    // Ability selection
                    int abilY = deckRowY + 44;
                    Raylib.DrawLine(dbMargin, abilY, width - dbMargin, abilY, new Color(35, 40, 52, 255));
                    abilY += 10;
                    Raylib.DrawText("ABILITY:", dbMargin, abilY + 8, 14, new Color(80, 185, 235, 255));
                    var allAbils = AbilityLibrary.GetAll();
                    int abilBtnW = (width - dbMargin * 2 - 70 - dbGap * (allAbils.Count - 1)) / allAbils.Count;
                    for (int i = 0; i < allAbils.Count; i++)
                    {
                        bool aSel = profile.SelectedAbility?.Id == allAbils[i].Id;
                        int ax = dbMargin + 70 + i * (abilBtnW + dbGap);
                        Rectangle aR = new Rectangle(ax, abilY, abilBtnW, 60);
                        Color aBody = aSel ? new Color(22, 44, 62, 255) : new Color(18, 20, 28, 255);
                        Color aBord = aSel ? new Color(80, 185, 235, 255) : new Color(48, 52, 70, 255);
                        bool aHov = Raylib.CheckCollisionPointRec(mouse, aR);
                        Raylib.DrawRectangleRounded(aR, 0.1f, 6, aHov ? new Color(26, 32, 42, 255) : aBody);
                        Raylib.DrawRectangleRoundedLinesEx(aR, 0.1f, 6, aSel ? 2.5f : 1.2f, aBord);
                        Raylib.DrawText(allAbils[i].Name, ax + 8, abilY + 8, 13, aSel ? Color.SkyBlue : Color.White);
                        Raylib.DrawText(allAbils[i].Description, ax + 8, abilY + 28, 11, new Color(130, 138, 158, 255));
                        if (aSel)
                        {
                            int selW = Raylib.MeasureText("SELECTED", 10);
                            Raylib.DrawText("SELECTED", ax + abilBtnW - selW - 6, abilY + 6, 10, Color.SkyBlue);
                        }
                        if (aHov && Raylib.IsMouseButtonPressed(MouseButton.Left))
                            profile.SelectedAbility = allAbils[i];
                    }
                    break;
                }

                case GameScene.RewardChoice:
                {
                    Raylib.DrawRectangle(0, 0, width, height, new Color(10, 12, 18, 255));
                    int rcHdrW = Raylib.MeasureText("CHOOSE A CARD", 32);
                    Raylib.DrawText("CHOOSE A CARD", width / 2 - rcHdrW / 2, 60, 32, Color.Gold);
                    int rcSubW = Raylib.MeasureText("Selected card is added to your collection", 16);
                    Raylib.DrawText("Selected card is added to your collection", width / 2 - rcSubW / 2, 104, 16, new Color(130, 135, 155, 255));

                    int rcCW = 240, rcCH = 320;
                    int rcTotal = rcCW * rewardOptions.Count + 40 * (rewardOptions.Count - 1);
                    int rcStartX = width / 2 - rcTotal / 2;
                    for (int ri = 0; ri < rewardOptions.Count; ri++)
                    {
                        var rc = rewardOptions[ri];
                        int rx = rcStartX + ri * (rcCW + 40);
                        int ry = height / 2 - rcCH / 2;
                        bool rcHov = Raylib.CheckCollisionPointRec(mouse, new Rectangle(rx, ry, rcCW, rcCH));
                        float lift = rcHov ? -16f : 0f;
                        Color rcBody = CardColorFromName(rc.Name);
                        rcBody = new Color((byte)(rcBody.R / 4), (byte)(rcBody.G / 4), (byte)(rcBody.B / 4), (byte)255);
                        DrawCard(new Rectangle(rx, ry + lift, rcCW, rcCH), rc.Name, rc.Description, rc.Cost,
                            rcBody, rcHov ? Color.Gold : new Color(65, 68, 88, 255), rcHov);

                        bool alreadyOwned = profile.TotalCollection.Any(c => c.Id == rc.Id);
                        if (alreadyOwned)
                        {
                            int owW = Raylib.MeasureText("OWNED", 11);
                            Raylib.DrawRectangle(rx + rcCW - owW - 10, (int)(ry + lift) + 4, owW + 8, 18, new Color(50, 140, 65, 200));
                            Raylib.DrawText("OWNED", rx + rcCW - owW - 6, (int)(ry + lift) + 6, 11, Color.White);
                        }

                        if (rcHov && Raylib.IsMouseButtonPressed(MouseButton.Left))
                        {
                            profile.TotalCollection.Add(rc);
                            Raylib.PlaySound(ui2Sound);
                            SaveGame(profile, completedNodes);
                            scene = GameScene.CampaignMap;
                        }
                    }

                    int skipW2 = Raylib.MeasureText("Skip reward", 13);
                    Raylib.DrawText("Skip reward", width / 2 - skipW2 / 2, height / 2 + rcCH / 2 + 30, 13, new Color(80, 84, 100, 255));
                    if (Raylib.CheckCollisionPointRec(mouse, new Rectangle(width / 2 - skipW2 / 2 - 4, height / 2 + rcCH / 2 + 26, skipW2 + 8, 22))
                        && Raylib.IsMouseButtonPressed(MouseButton.Left))
                    {
                        SaveGame(profile, completedNodes);
                        scene = GameScene.CampaignMap;
                    }
                    break;
                }

                case GameScene.CampaignVictory:
                {
                    Raylib.DrawRectangle(0, 0, width, height, new Color(6, 10, 6, 255));
                    // Starfield
                    for (int si = 0; si < 120; si++)
                    {
                        int sx = (si * 137 + 41) % width;
                        int sy = (si * 179 + 83) % height;
                        int sa = 100 + si % 155;
                        Raylib.DrawPixel(sx, sy, new Color(200, 220, 200, sa));
                    }
                    int vcW = Raylib.MeasureText("RUN COMPLETE", 52);
                    Raylib.DrawText("RUN COMPLETE", width / 2 - vcW / 2, height / 2 - 140, 52, Color.Gold);
                    int vcSW = Raylib.MeasureText("THE CATALYST SINGULARITY DEFEATED", 20);
                    Raylib.DrawText("THE CATALYST SINGULARITY DEFEATED", width / 2 - vcSW / 2, height / 2 - 76, 20, new Color(188, 220, 188, 255));
                    Raylib.DrawLineEx(new Vector2(width / 2 - 220f, height / 2 - 46f), new Vector2(width / 2 + 220f, height / 2 - 46f), 1.5f, new Color(80, 160, 80, 160));
                    int vcMaxHp = 50 + profile.MaxHpBonus;
                    int vcHpLeft = profile.PlayerHp > 0 ? profile.PlayerHp : vcMaxHp;
                    string vcStats = $"HP remaining: {vcHpLeft}/{vcMaxHp}   Gold: {profile.Gold} G   Cards collected: {profile.TotalCollection.Count}";
                    int vcStW = Raylib.MeasureText(vcStats, 14);
                    Raylib.DrawText(vcStats, width / 2 - vcStW / 2, height / 2 - 26, 14, new Color(140, 185, 140, 255));
                    if (DrawButton(new Rectangle(width / 2 - 150, height / 2 + 14, 300, 52), "START NEW RUN", new Color(28, 62, 28, 255), new Color(42, 98, 42, 255)))
                    {
                        profile.Gold = 150;
                        profile.SkillPoints = 8;
                        profile.PlayerHp = 0;
                        completedNodes.Clear();
                        foreach (var cn in campaignNodes) cn.Completed = false;
                        SaveGame(profile, completedNodes);
                        scene = GameScene.TitleScreen;
                    }
                    break;
                }
            }

            // Burn pile overlay
            if (showBurnPile && scene == GameScene.BattleView && !isPaused)
            {
                var bsOv = battleService.State;
                Raylib.DrawRectangle(0, 0, width, height, new Color(0, 0, 0, 195));
                int ovW = Math.Min(680, width - 60);
                int ovH = Math.Min(460, height - 80);
                int ovX = width / 2 - ovW / 2;
                int ovY = height / 2 - ovH / 2;
                Raylib.DrawRectangleRounded(new Rectangle(ovX, ovY, ovW, ovH), 0.06f, 6, new Color(14, 16, 24, 255));
                Raylib.DrawRectangleRoundedLinesEx(new Rectangle(ovX, ovY, ovW, ovH), 0.06f, 6, 1.5f, new Color(70, 75, 100, 200));
                string ovTitle = $"DISCARD PILE  ({bsOv.BurnPile.Count} cards)";
                int ovTW = Raylib.MeasureText(ovTitle, 20);
                Raylib.DrawText(ovTitle, width / 2 - ovTW / 2, ovY + 16, 20, Color.White);
                Raylib.DrawLine(ovX + 20, ovY + 48, ovX + ovW - 20, ovY + 48, new Color(55, 60, 80, 255));
                if (bsOv.BurnPile.Count == 0)
                {
                    int emW = Raylib.MeasureText("Empty", 16);
                    Raylib.DrawText("Empty", width / 2 - emW / 2, height / 2 - 8, 16, new Color(78, 82, 100, 255));
                }
                else
                {
                    int chipW = 148, chipH = 30, chipGap = 8;
                    int cols = Math.Max(1, (ovW - 24) / (chipW + chipGap));
                    for (int ci = 0; ci < bsOv.BurnPile.Count; ci++)
                    {
                        int row = ci / cols, col = ci % cols;
                        int cx2 = ovX + 12 + col * (chipW + chipGap);
                        int cy2 = ovY + 58 + row * (chipH + chipGap);
                        if (cy2 + chipH > ovY + ovH - 36) break;
                        var cd = bsOv.BurnPile[ci];
                        Color chCol = CardColorFromName(cd.Name);
                        Raylib.DrawRectangleRounded(new Rectangle(cx2, cy2, chipW, chipH), 0.28f, 4,
                            new Color((byte)(chCol.R / 6), (byte)(chCol.G / 6), (byte)(chCol.B / 6), (byte)255));
                        Raylib.DrawRectangleRoundedLinesEx(new Rectangle(cx2, cy2, chipW, chipH), 0.28f, 4, 1.2f, chCol);
                        Raylib.DrawText(cd.Name, cx2 + 8, cy2 + 9, 11, Color.White);
                        int costTW = Raylib.MeasureText($"[{cd.Cost}]", 11);
                        Raylib.DrawText($"[{cd.Cost}]", cx2 + chipW - costTW - 7, cy2 + 9, 11, new Color(80, 180, 235, 255));
                    }
                }
                int hintW2 = Raylib.MeasureText("Click anywhere or press ESC to close", 12);
                Raylib.DrawText("Click anywhere or press ESC to close", width / 2 - hintW2 / 2, ovY + ovH - 24, 12, new Color(70, 75, 98, 255));
                if (Raylib.IsMouseButtonPressed(MouseButton.Left))
                    showBurnPile = false;
            }

            // Pause overlay (BattleView only)
            if (isPaused && scene == GameScene.BattleView)
            {
                Raylib.DrawRectangle(0, 0, width, height, new Color((byte)0, (byte)0, (byte)0, (byte)185));
                int pnW = 360, pnH = 270;
                int pnX = width / 2 - pnW / 2, pnY = height / 2 - pnH / 2;
                Raylib.DrawRectangleRounded(new Rectangle(pnX, pnY, pnW, pnH), 0.1f, 6, new Color(14, 16, 24, 255));
                Raylib.DrawRectangleRoundedLinesEx(new Rectangle(pnX, pnY, pnW, pnH), 0.1f, 6, 1.5f, new Color(80, 90, 120, 200));
                int pauseHdrW = Raylib.MeasureText("PAUSED", 36);
                Raylib.DrawText("PAUSED", width / 2 - pauseHdrW / 2, pnY + 18, 36, Color.White);
                Raylib.DrawLine(pnX + 20, pnY + 64, pnX + pnW - 20, pnY + 64, new Color(55, 60, 80, 255));

                // Volume slider
                Raylib.DrawText("VOLUME", pnX + 24, pnY + 80, 14, new Color(155, 160, 178, 255));
                float slX = pnX + 112, slY = pnY + 82f, slW = pnW - 138f, slH = 14f;
                Raylib.DrawRectangleRec(new Rectangle(slX, slY, slW, slH), new Color(28, 32, 44, 255));
                Raylib.DrawRectangleRec(new Rectangle(slX, slY, slW * masterVolume, slH), new Color(80, 148, 222, 255));
                Raylib.DrawRectangleLinesEx(new Rectangle(slX, slY, slW, slH), 1f, new Color(55, 60, 80, 255));
                Raylib.DrawCircleV(new Vector2(slX + slW * masterVolume, slY + slH / 2), 9f, new Color(120, 175, 235, 255));
                if (Raylib.IsMouseButtonDown(MouseButton.Left) &&
                    Raylib.CheckCollisionPointRec(mouse, new Rectangle(slX - 10, slY - 12, slW + 20, slH + 24)))
                {
                    masterVolume = Math.Clamp((mouse.X - slX) / slW, 0f, 1f);
                    Raylib.SetMasterVolume(masterVolume);
                }

                if (DrawButton(new Rectangle(width / 2 - 130, pnY + 120, 260, 46), "RESUME",
                        new Color(38, 50, 72, 255), new Color(55, 78, 115, 255)))
                    isPaused = false;

                if (DrawButton(new Rectangle(width / 2 - 130, pnY + 180, 260, 46), "QUIT TO TITLE",
                        new Color(72, 28, 28, 255), new Color(108, 42, 42, 255)))
                {
                    SaveGame(profile, completedNodes);
                    isPaused = false;
                    scene = GameScene.TitleScreen;
                }
            }

            Raylib.EndMode2D();
            Raylib.EndDrawing();
        }

        foreach (var s in whooshSounds) Raylib.UnloadSound(s);
        foreach (var s in hitSounds)   Raylib.UnloadSound(s);
        Raylib.UnloadSound(whoosh6); Raylib.UnloadSound(whoosh7);
        Raylib.UnloadSound(winSound); Raylib.UnloadSound(loseSound);
        Raylib.UnloadSound(ui1Sound); Raylib.UnloadSound(ui2Sound);
        Raylib.CloseAudioDevice();
        Raylib.CloseWindow();
    }
}
