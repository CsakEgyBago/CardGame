using System.Numerics;
using Raylib_cs;
using CardGamePrototype.Core;

namespace CardGamePrototype.Client;

public enum GameScene { TitleScreen, ModeSelect, CampaignMap, BattleView, MarketShop, DeckBuilder, RewardChoice }
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
    public List<CardDefinition> TotalCollection { get; set; } = new();
    public List<CardDefinition> ActiveDeck { get; set; } = new();
    public SkillTreeManager SkillTree { get; set; } = new();
    public AbilityDefinition? SelectedAbility { get; set; }

    bool HasNode(int id) => SkillTree.Nodes.Find(n => n.Id == id)?.IsUnlocked == true;

    public int MaxEnergyBonus  => (HasNode(1) ? 1 : 0) + (HasNode(2) ? 1 : 0) + (HasNode(3) ? 2 : 0);
    public int MaxHpBonus      => (HasNode(11) ? 10 : 0) + (HasNode(12) ? 15 : 0) + (HasNode(13) ? 25 : 0);
    public int StartCardsBonus => (HasNode(4) ? 1 : 0) + (HasNode(5) ? 1 : 0);
}

class Program
{
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
            for (int x = 20; x < width; x += 36)
                for (int y = 20; y < height; y += 36)
                    Raylib.DrawCircleV(new Vector2(x, y), 0.9f, new Color(38, 42, 54, 255));
        }
        else
        {
            Raylib.ClearBackground(new Color(20, 14, 8, 255));
            for (int x = 0; x < width; x += 64) Raylib.DrawLine(x, 0, x, height, new Color(30, 22, 12, 255));
            for (int y = 0; y < height; y += 48) Raylib.DrawLine(0, y, width, y, new Color(26, 18, 10, 255));
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
        Random sfxRng = new Random();

        bool dragging = false;
        int draggingIndex = -1;
        Vector2 dragOffset = Vector2.Zero;
        bool isDraggingFromCollectionPool = false;
        List<FloatDmg> floatDmgs = new();
        float enemyFlashTimer = 0f;
        Vector2 enemyScreenPos = Vector2.Zero;

        while (!Raylib.WindowShouldClose())
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

            Raylib.BeginDrawing();
            if (theme == GameTheme.SciFi)
                Raylib.ClearBackground(new Color(11, 13, 18, 255));
            else
                Raylib.ClearBackground(new Color(20, 14, 8, 255));
            DrawBackground(width, height, theme);

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
                            battleService.State.Player.Hp    = finalMaxHp;
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
                            scene = GameScene.BattleView;
                        }
                    }
                    break;

                case GameScene.BattleView:
                    var bs = battleService.State;

                    // Consume damage log → spawn float numbers + trigger flash + play hit sounds
                    if (bs.DamageLog.Count > 0)
                    {
                        bool hitEnemy = bs.DamageLog.Any(e => e.Amount > 0 && e.Tag.StartsWith("enemy"));
                        bool hitPlayer = bs.DamageLog.Any(e => e.Amount < 0);
                        if (hitEnemy)  { enemyFlashTimer = 0.20f; Raylib.PlaySound(hitSounds[sfxRng.Next(4)]); }
                        if (hitPlayer) Raylib.PlaySound(hitSounds[sfxRng.Next(4)]);
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

                    if (bs.Phase == TurnPhase.PlayerTurn)
                    {
                        if (Raylib.IsKeyPressed(KeyboardKey.Enter))
                            battleService.EndTurn();
                        if (Raylib.IsKeyPressed(KeyboardKey.Space) && bs.SelectedBoardSlot >= 0)
                            battleService.ExecuteCard(bs.SelectedBoardSlot);
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
                                Rectangle hcr = new Rectangle(10, 85 + j * (cpH + 6), cpW, cpH);
                                if (Raylib.CheckCollisionPointRec(mouse, hcr)) { hovSF = j; break; }
                            }

                        // Left sidebar: Player
                        Raylib.DrawRectangle(0, 0, SW, height, new Color(18, 20, 25, 255));
                        Raylib.DrawLine(SW, 0, SW, height, new Color(50, 55, 65, 255));
                        Raylib.DrawText("PLAYER", 10, 8, 12, new Color(100, 105, 128, 255));
                        Raylib.DrawText($"{bs.Player.Hp} / {bs.Player.MaxHp}", 10, 26, 14, Color.White);
                        DrawBar(10, 44, SW - 22, 6, (float)bs.Player.Hp / Math.Max(bs.Player.MaxHp, 1), new Color(48, 200, 88, 255), new Color(28, 32, 40, 255));
                        Raylib.DrawText($"NRG  {bs.Player.Energy}", 10, 56, 13, new Color(80, 185, 235, 255));
                        Raylib.DrawText($"DIS  {bs.BurnPile.Count}", 110, 56, 13, new Color(205, 140, 48, 255));
                        Raylib.DrawLine(0, 74, SW, 74, new Color(35, 40, 52, 255));

                        for (int i = 0; i < bs.Hand.Count; i++)
                        {
                            if (dragging && i == draggingIndex) continue;
                            Rectangle cr = new Rectangle(10, 85 + i * (cpH + 6), cpW, cpH);
                            var hc = bs.Hand[i];
                            DrawCard(cr, hc.Name, hc.Description, hc.Cost, new Color(20, 24, 34, 255), i == hovSF ? Color.Gold : new Color(50, 55, 70, 255));
                            if (Raylib.IsMouseButtonPressed(MouseButton.Left) && Raylib.CheckCollisionPointRec(mouse, cr) && !dragging)
                            {
                                dragging = true; draggingIndex = i; isDraggingFromCollectionPool = false;
                                dragOffset = mouse - new Vector2(cr.X, cr.Y);
                            }
                        }

                        int sbBot = height - 54;
                        Raylib.DrawLine(0, sbBot, SW, sbBot, new Color(35, 40, 52, 255));
                        Raylib.DrawText($"HAND {bs.Hand.Count}", 10, sbBot + 8, 12, new Color(130, 134, 152, 255));
                        Raylib.DrawText($"DECK {bs.DrawPile.Count}", 10, sbBot + 26, 12, new Color(95, 100, 118, 255));

                        // Right sidebar: Enemy / Board
                        Raylib.DrawRectangle(width - SW, 0, SW, height, new Color(18, 20, 25, 255));
                        Raylib.DrawLine(width - SW, 0, width - SW, height, new Color(50, 55, 65, 255));
                        Raylib.DrawText("ENEMY", width - SW + 8, 8, 12, new Color(100, 105, 128, 255));
                        int enw2 = Raylib.MeasureText(activeBattleNode?.Name ?? "Enemy", 12);
                        Raylib.DrawText(activeBattleNode?.Name ?? "Enemy", Math.Min(width - SW + 68, width - 8 - enw2), 8, 12, new Color(195, 85, 75, 255));
                        Raylib.DrawText($"{bs.Enemy.Hp} / {bs.Enemy.MaxHp}", width - SW + 8, 26, 14, Color.White);
                        DrawBar(width - SW + 8, 44, SW - 22, 6, (float)bs.Enemy.Hp / Math.Max(bs.Enemy.MaxHp, 1), new Color(200, 55, 55, 255), new Color(28, 32, 40, 255));
                        int fS = bs.Enemy.ActiveElements.GetStacks(ElementType.Fire);
                        int frS = bs.Enemy.ActiveElements.GetStacks(ElementType.Frost);
                        if (fS  > 0) Raylib.DrawText($"Fire {fS}",  width - SW + 8,  56, 12, new Color(225, 115, 38, 255));
                        if (frS > 0) Raylib.DrawText($"Frost {frS}", width - SW + 82, 56, 12, new Color(88, 185, 235, 255));
                        // Enemy intent
                        {
                            var (intentSF, dmgSF, laneSF) = TurnManager.GetEnemyIntent(bs);
                            int inW = Raylib.MeasureText(intentSF, 10);
                            int inX = width - SW + Math.Max(0, (SW - inW) / 2);
                            Raylib.DrawText(intentSF, inX, 70, 10, new Color(210, 100, 60, 200));
                        }
                        Raylib.DrawLine(width - SW, 82, width, 82, new Color(35, 40, 52, 255));

                        for (int i = 0; i < bs.PlayerBoard.Count; i++)
                        {
                            var bSlot = bs.PlayerBoard[i];
                            bool bSel = bs.SelectedBoardSlot == i;
                            Rectangle sr = new(width - SW + 10, 88 + i * (cpH + 6), cpW, cpH);
                            if (bSlot.IsOccupied)
                            {
                                var occ = bSlot.Occupant!;
                                DrawCard(sr, occ.SourceCard.Name, $"ATK {occ.BaseAttack}  HP {occ.Hp}/{occ.MaxHp}", occ.SourceCard.Cost, new Color(22, 26, 38, 255), bSel ? Color.Gold : Color.SkyBlue, bSel);
                            }
                            else
                            {
                                Raylib.DrawRectangleRec(sr, new Color(28, 31, 38, 200));
                                Raylib.DrawRectangleLinesEx(sr, 1, bSel ? Color.Gold : new Color(50, 55, 65, 255));
                                Raylib.DrawText($"Lane {i}", (int)sr.X + 8, (int)(sr.Y + sr.Height / 2 - 7), 13, new Color(60, 65, 78, 180));
                            }
                            if (Raylib.IsMouseButtonPressed(MouseButton.Left) && Raylib.CheckCollisionPointRec(mouse, sr))
                                bs.SelectedBoardSlot = bSel ? -1 : i;
                        }

                        // Center grid (always visible)
                        int fTop = 70, fBot = height - 100;
                        int tL = gcx + gcw / 10, tR = gcx + gcw - gcw / 10;
                        int bL = gcx + 8, bR = gcx + gcw - 8;

                        Raylib.DrawRectangle(gcx, fTop, gcw, fBot - fTop, new Color(28, 32, 40, 255));
                        Raylib.DrawLineEx(new Vector2(tL, fTop), new Vector2(tR, fTop), 2, new Color(80, 88, 105, 220));
                        Raylib.DrawLineEx(new Vector2(bL, fBot), new Vector2(bR, fBot), 2, new Color(80, 88, 105, 220));

                        for (int i = 0; i <= 5; i++)
                        {
                            float tf = (float)i / 5;
                            int topX = (int)(tL + tf * (tR - tL));
                            int botX = (int)(bL + tf * (bR - bL));
                            Raylib.DrawLineEx(new Vector2(topX, fTop), new Vector2(botX, fBot), i == 0 || i == 5 ? 2 : 1, new Color(58, 63, 76, 210));
                        }

                        int midY = fTop + (int)((fBot - fTop) * 0.48f);
                        int midL = (int)(tL + 0.48f * (bL - tL));
                        int midR = (int)(tR + 0.48f * (bR - tR));
                        Raylib.DrawLineEx(new Vector2(midL, midY), new Vector2(midR, midY), 1, new Color(55, 60, 74, 190));

                        int ezLW = Raylib.MeasureText("ENEMY ZONE", 11);
                        Raylib.DrawText("ENEMY ZONE", gcx + (gcw - ezLW) / 2, fTop + 6, 11, new Color(140, 50, 50, 200));
                        int dzLW = Raylib.MeasureText("DEPLOY ZONE", 11);
                        Raylib.DrawText("DEPLOY ZONE", gcx + (gcw - dzLW) / 2, fBot - 20, 11, new Color(50, 130, 160, 200));

                        for (int i = 0; i < 5; i++)
                        {
                            float t0 = (float)i / 5, t1 = (float)(i + 1) / 5;
                            int ex0 = (int)(tL + t0 * (tR - tL)) + 4, ex1 = (int)(tL + t1 * (tR - tL)) - 4;
                            int px0 = (int)(bL + t0 * (bR - bL)) + 4, px1 = (int)(bL + t1 * (bR - bL)) - 4;

                            Rectangle dropZone = new Rectangle(px0, midY + 4, px1 - px0, fBot - midY - 8);
                            bool isTarget = dragging && Raylib.CheckCollisionPointRec(mouse, dropZone);
                            Raylib.DrawRectangleLinesEx(dropZone, isTarget ? 3 : 1, isTarget ? Color.Gold : new Color(60, 65, 78, 220));
                            if (isTarget) Raylib.DrawRectangleRec(new Rectangle(dropZone.X + 1, dropZone.Y + 1, dropZone.Width - 2, dropZone.Height - 2), new Color(255, 215, 50, 28));

                            Rectangle enemyZone = new Rectangle(ex0, fTop + 4, ex1 - ex0, midY - fTop - 8);
                            bool hasEnemy = bs.Enemy.Position == i;
                            if (hasEnemy)
                            {
                                if (bs.Enemy.ActiveElements.GetStacks(ElementType.Fire) > 0)
                                    Raylib.DrawRectangleLinesEx(new Rectangle(enemyZone.X - 3, enemyZone.Y - 3, enemyZone.Width + 6, enemyZone.Height + 6), 2, Color.Orange);
                                if (bs.Enemy.ActiveElements.GetStacks(ElementType.Frost) > 0)
                                    Raylib.DrawRectangleLinesEx(new Rectangle(enemyZone.X - 6, enemyZone.Y - 6, enemyZone.Width + 12, enemyZone.Height + 12), 2, Color.SkyBlue);
                                Raylib.DrawRectangleRounded(enemyZone, 0.06f, 4, Color.Maroon);
                                int ehpW2 = Raylib.MeasureText($"{bs.Enemy.Hp}", 12);
                                Raylib.DrawText($"{bs.Enemy.Hp}", (int)(enemyZone.X + (enemyZone.Width - ehpW2) / 2), (int)(enemyZone.Y + enemyZone.Height / 2 - 7), 12, Color.White);
                                // Hit flash overlay
                                if (enemyFlashTimer > 0)
                                {
                                    byte fa = (byte)Math.Min(200, (int)(enemyFlashTimer * 5 * 255));
                                    Raylib.DrawRectangleRounded(enemyZone, 0.06f, 4, new Color((byte)255, (byte)255, (byte)255, fa));
                                }
                                enemyScreenPos = new Vector2(enemyZone.X + enemyZone.Width / 2f, enemyZone.Y + enemyZone.Height / 2f);
                            }
                            else
                            {
                                Raylib.DrawRectangleLinesEx(enemyZone, 1, new Color(48, 52, 63, 180));
                            }

                            if (i < bs.PlayerBoard.Count && bs.PlayerBoard[i].IsOccupied)
                            {
                                var occ = bs.PlayerBoard[i].Occupant!;
                                bool uSel = bs.SelectedBoardSlot == i;
                                DrawSummonedUnit(new Rectangle(dropZone.X + 2, dropZone.Y + 2, dropZone.Width - 4, dropZone.Height - 4), occ.SourceCard.Name, occ.Hp, occ.MaxHp, uSel);
                                int aLblW = Raylib.MeasureText($"ATK {occ.BaseAttack}", 10);
                                Raylib.DrawText($"ATK {occ.BaseAttack}", (int)(dropZone.X + (dropZone.Width - aLblW) / 2), (int)(dropZone.Y + 4), 10, new Color(80, 200, 235, 255));
                            }
                        }

                        // Drop release
                        if (!Raylib.IsMouseButtonDown(MouseButton.Left) && dragging && !isDraggingFromCollectionPool)
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
                            if (Raylib.IsMouseButtonPressed(MouseButton.Left) && Raylib.CheckCollisionPointRec(mouse, pvRect))
                            {
                                dragging = true; draggingIndex = hovSF; isDraggingFromCollectionPool = false;
                                dragOffset = mouse - new Vector2(pvRect.X, pvRect.Y);
                            }
                        }

                        // Phase indicator + END TURN
                        bool isPlayerTurnSF = bs.Phase == TurnPhase.PlayerTurn;
                        string phaseLblSF = isPlayerTurnSF ? "YOUR TURN" : "ENEMY TURN";
                        Color phaseColSF = isPlayerTurnSF ? new Color(88, 200, 110, 255) : new Color(210, 80, 68, 255);
                        int phWSF = Raylib.MeasureText(phaseLblSF, 14);
                        Raylib.DrawRectangle(gcx + gcw / 2 - phWSF / 2 - 14, height - 102, phWSF + 28, 20, new Color(14, 16, 22, 200));
                        Raylib.DrawText(phaseLblSF, gcx + gcw / 2 - phWSF / 2, height - 100, 14, phaseColSF);
                        if (isPlayerTurnSF && DrawButton(new Rectangle(gcx + gcw / 2 - 88, height - 78, 176, 40), "END TURN", new Color(42, 68, 36, 255), new Color(60, 100, 52, 255)))
                            battleService.EndTurn();

                        // Ability bar
                        if (bs.EquippedAbility != null)
                        {
                            if (DrawAbilityBar(gcx + 8, height - 72, gcw - 16, 30, bs, isPlayerTurnSF))
                                battleService.ActivateAbility();
                        }

                        Raylib.DrawRectangle(gcx, height - 34, gcw, 34, new Color(10, 11, 16, 220));
                        Raylib.DrawLine(gcx, height - 34, gcx + gcw, height - 34, new Color(32, 36, 48, 255));
                        int hintWSF = Raylib.MeasureText("[ENTER] End Turn   ·   [SPACE] Execute   ·   Drag card to play", 12);
                        Raylib.DrawText("[ENTER] End Turn   ·   [SPACE] Execute   ·   Drag card to play", gcx + gcw / 2 - hintWSF / 2, height - 22, 12, new Color(72, 76, 96, 255));

                        // Drag ghost
                        if (dragging && draggingIndex >= 0 && draggingIndex < bs.Hand.Count)
                        {
                            var dc = bs.Hand[draggingIndex];
                            DrawCard(new Rectangle(mouse.X - dragOffset.X, mouse.Y - dragOffset.Y, 155, 205), dc.Name, dc.Description, dc.Cost, new Color(255, 250, 210, 230), Color.Gold);
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
                        DrawBar(12, 14, 200, 10, (float)bs.Player.Hp / Math.Max(bs.Player.MaxHp, 1), new Color(60, 200, 80, 255), new Color(22, 30, 18, 255));
                        int hpFW = Raylib.MeasureText($"{bs.Player.Hp}/{bs.Player.MaxHp}", 12);
                        Raylib.DrawText($"{bs.Player.Hp}/{bs.Player.MaxHp}", 12 + 100 - hpFW / 2, 28, 12, fpText);
                        Raylib.DrawText($"NRG {bs.Player.Energy}", 220, 14, 13, new Color(80, 185, 235, 255));
                        Raylib.DrawText($"DIS {bs.BurnPile.Count}", 220, 32, 12, new Color(205, 140, 48, 255));
                        // Header (0-54px)
                        Raylib.DrawRectangle(0, 0, width, 54, new Color(22, 15, 8, 255));
                        Raylib.DrawLine(0, 54, width, 54, new Color(105, 78, 42, 180));
                        DrawBar(12, 10, 180, 9, (float)bs.Player.Hp / Math.Max(bs.Player.MaxHp, 1), new Color(60, 200, 80, 255), new Color(22, 30, 18, 255));
                        int fpHpFW = Raylib.MeasureText($"{bs.Player.Hp}/{bs.Player.MaxHp}", 11);
                        Raylib.DrawText($"{bs.Player.Hp}/{bs.Player.MaxHp}", 12 + 90 - fpHpFW / 2, 23, 11, fpText);
                        Raylib.DrawText($"NRG {bs.Player.Energy}", 200, 8, 12, new Color(80, 185, 235, 255));
                        Raylib.DrawText($"DIS {bs.BurnPile.Count}", 200, 26, 11, new Color(205, 140, 48, 255));
                        bool isPlayerTurnFP = bs.Phase == TurnPhase.PlayerTurn;
                        string phaseLblFP = isPlayerTurnFP ? "YOUR TURN" : "ENEMY TURN";
                        Color phaseColFP = isPlayerTurnFP ? new Color(88, 200, 110, 255) : new Color(210, 80, 68, 255);
                        int phWFP = Raylib.MeasureText(phaseLblFP, 13);
                        Raylib.DrawText(phaseLblFP, width / 2 - phWFP / 2, 18, 13, phaseColFP);
                        if (isPlayerTurnFP && DrawButton(new Rectangle(width - 178, 7, 168, 40), "END TURN", new Color(55, 36, 18, 255), new Color(88, 60, 28, 255)))
                            battleService.EndTurn();

                        // Enemy panel (58 to height*0.20)
                        int epBot = (int)(height * 0.20f);
                        Raylib.DrawRectangleRounded(new Rectangle(width / 2 - 280, 58, 560, epBot - 64), 0.08f, 6, fpPanel);
                        Raylib.DrawRectangleRoundedLinesEx(new Rectangle(width / 2 - 280, 58, 560, epBot - 64), 0.08f, 6, 1.5f, fpBord);
                        string eName = activeBattleNode?.Name ?? "Enemy";
                        int eNW = Raylib.MeasureText(eName, 15);
                        Raylib.DrawText(eName, width / 2 - eNW / 2, 66, 15, new Color(200, 85, 75, 255));
                        DrawBar(width / 2 - 180, 88, 360, 9, (float)bs.Enemy.Hp / Math.Max(bs.Enemy.MaxHp, 1), new Color(200, 55, 55, 255), new Color(28, 22, 18, 255));
                        int eHpW = Raylib.MeasureText($"{bs.Enemy.Hp} / {bs.Enemy.MaxHp}", 11);
                        Raylib.DrawText($"{bs.Enemy.Hp} / {bs.Enemy.MaxHp}", width / 2 - eHpW / 2, 101, 11, fpText);
                        var (intentStr, intentDmg, intentLane) = TurnManager.GetEnemyIntent(bs);
                        int intentW = Raylib.MeasureText(intentStr, 12);
                        bool intentIsAttack = intentStr.StartsWith("ATTACK");
                        Raylib.DrawText(intentStr, width / 2 - intentW / 2, epBot - 36, 12,
                            intentIsAttack ? new Color(215, 80, 65, 255) : new Color(225, 185, 50, 255));
                        int eFire = bs.Enemy.ActiveElements.GetStacks(ElementType.Fire);
                        int eFrost = bs.Enemy.ActiveElements.GetStacks(ElementType.Frost);
                        if (eFire  > 0) Raylib.DrawText($"Fire {eFire}",  width / 2 - 120, epBot - 18, 11, new Color(225, 115, 38, 255));
                        if (eFrost > 0) Raylib.DrawText($"Frost {eFrost}", width / 2 + 60,  epBot - 18, 11, new Color(88, 185, 235, 255));

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
                                battleService.ActivateAbility();
                        }

                        // 2.5D layered battlefield — visible when hovering a pile or dragging
                        if (showGrid)
                        {
                            int bL3 = 0, bR3 = width;
                            int bTop3 = epBot + 4;
                            int bBot3 = showHandCards ? stripY - 134 : stripY - 4;
                            float bH3 = bBot3 - bTop3;

                            // Layer 1: ceiling / sky
                            Raylib.DrawRectangleGradientV(bL3, bTop3, bR3, (int)(bH3 * 0.28f),
                                new Color(8, 5, 14, 255), new Color(18, 11, 22, 255));
                            // Layer 2: stone back wall
                            Raylib.DrawRectangleGradientV(bL3, bTop3 + (int)(bH3 * 0.28f), bR3, (int)(bH3 * 0.24f),
                                new Color(18, 11, 22, 255), new Color(36, 24, 15, 255));
                            // Layer 3: mid-ground floor
                            Raylib.DrawRectangleGradientV(bL3, bTop3 + (int)(bH3 * 0.52f), bR3, bBot3 - (bTop3 + (int)(bH3 * 0.52f)),
                                new Color(36, 24, 15, 255), new Color(58, 40, 22, 255));

                            // Stone pillars (atmosphere)
                            for (int ci = 1; ci < 5; ci++)
                            {
                                int pcx = bL3 + (bR3 - bL3) * ci / 5;
                                Raylib.DrawRectangle(pcx - 10, bTop3 + (int)(bH3 * 0.06f), 20, (int)(bH3 * 0.47f), new Color(24, 15, 9, 210));
                                Raylib.DrawLine(pcx - 10, bTop3 + (int)(bH3 * 0.06f), pcx - 10, bTop3 + (int)(bH3 * 0.53f), new Color(40, 28, 16, 90));
                                Raylib.DrawLine(pcx + 10, bTop3 + (int)(bH3 * 0.06f), pcx + 10, bTop3 + (int)(bH3 * 0.53f), new Color(40, 28, 16, 90));
                            }
                            // Torch glow at each pillar
                            for (int ci = 1; ci < 5; ci++)
                            {
                                float tcx = bL3 + (bR3 - bL3) * ci / 5f;
                                int tcy3 = bTop3 + (int)(bH3 * 0.35f);
                                Raylib.DrawCircleV(new Vector2(tcx, tcy3), 38, new Color(220, 140, 40, 10));
                                Raylib.DrawCircleV(new Vector2(tcx, tcy3), 22, new Color(220, 160, 60, 14));
                                Raylib.DrawCircleV(new Vector2(tcx, tcy3), 8,  new Color(240, 200, 80, 28));
                            }
                            // Ceiling shadow
                            Raylib.DrawRectangleGradientV(bL3, bTop3, bR3, 20, new Color(0, 0, 0, 220), new Color(0, 0, 0, 0));

                            // 5 lane rows: far (top/thin/dim) → near (bottom/thick/bright)
                            float lanesTop3 = bTop3 + bH3 * 0.48f;
                            float lanesEnd3 = bBot3 - 1f;
                            float lanesH3   = lanesEnd3 - lanesTop3;
                            float[] lProps   = { 0.14f, 0.16f, 0.18f, 0.24f, 0.28f };
                            float[] laneY    = new float[6];
                            laneY[0] = lanesTop3;
                            for (int li = 0; li < 5; li++) laneY[li + 1] = laneY[li] + lanesH3 * lProps[li];

                            Color[] laneFill3 = {
                                new Color(28, 17, 10, 255), new Color(33, 22, 13, 255),
                                new Color(39, 27, 15, 255), new Color(46, 31, 17, 255), new Color(55, 38, 20, 255)
                            };
                            float[] scaleL = { 0.50f, 0.62f, 0.74f, 0.87f, 1.00f };
                            float splitX3 = width * 0.38f;

                            // Draw lane rows (back to front)
                            for (int li = 0; li < 5; li++)
                            {
                                float lT3 = laneY[li], lB3 = laneY[li + 1];
                                Raylib.DrawRectangle(bL3, (int)lT3, bR3, (int)(lB3 - lT3) + 1, laneFill3[li]);
                                Raylib.DrawLine(bL3, (int)lT3, bR3, (int)lT3, new Color(60, 40, 20, li == 0 ? 200 : 130));
                                // Subtle floor tiles
                                for (float fy3 = lT3 + 10; fy3 < lB3 - 4; fy3 += 16)
                                    Raylib.DrawLine(bL3, (int)fy3, (int)splitX3, (int)fy3, new Color(20, 13, 7, 45));
                                // Lane number
                                Raylib.DrawText($"L{li}", 5, (int)(lT3 + (lB3 - lT3) / 2 - 6), 11, new Color(62, 48, 26, 130));
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
                                    if (executeMode)
                                    {
                                        if (Raylib.CheckCollisionPointRec(mouse, deployHitFP))
                                            Raylib.DrawRectangleRec(deployHitFP, new Color(80, 185, 235, 22));
                                        if (Raylib.CheckCollisionPointRec(mouse, deployHitFP) && Raylib.IsMouseButtonPressed(MouseButton.Left))
                                            battleService.ExecuteCard(li);
                                    }
                                    else if (Raylib.IsMouseButtonPressed(MouseButton.Left) && Raylib.CheckCollisionPointRec(mouse, deployHitFP))
                                        bs.SelectedBoardSlot = bs.SelectedBoardSlot == li ? -1 : li;
                                }
                                else if (!isDeployTgt)
                                {
                                    int emW3 = Raylib.MeasureText("—", (int)(11 * sc3));
                                    Raylib.DrawText("—", (int)(splitX3 / 2 - emW3 / 2), (int)(lT3 + lHx / 2 - 6 * sc3), (int)(11 * sc3), new Color(55, 44, 26, 110));
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
                            if (!Raylib.IsMouseButtonDown(MouseButton.Left) && dragging && !isDraggingFromCollectionPool)
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
                                    if (Raylib.IsMouseButtonPressed(MouseButton.Left) && Raylib.CheckCollisionPointRec(mouse, new Rectangle(12 + i * hcSp3, hcY3, hcW3, hcH3)) && !dragging)
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

                    // Win/Lose overlay (both themes)
                    if (bs.Phase == TurnPhase.Finished)
                    {
                        Raylib.DrawRectangle(0, 0, width, height, new Color(8, 9, 13, 215));
                        if (bs.Enemy.IsDead)
                        {
                            Raylib.DrawRectangle(width / 2 - 220, height / 2 - 80, 440, 170, new Color(18, 22, 16, 240));
                            Raylib.DrawRectangleLinesEx(new Rectangle(width / 2 - 220, height / 2 - 80, 440, 170), 1.5f, new Color(72, 185, 88, 160));
                            int vcw = Raylib.MeasureText("VICTORY", 56);
                            Raylib.DrawText("VICTORY", width / 2 - vcw / 2, height / 2 - 72, 56, Color.Gold);
                            Raylib.DrawLineEx(new Vector2(width / 2 - 180f, height / 2 - 8f), new Vector2(width / 2 + 180f, height / 2 - 8f), 1f, new Color(68, 155, 78, 160));
                            int r1w2 = Raylib.MeasureText("+60 Credits  —  choose a card reward", 15);
                            Raylib.DrawText("+60 Credits  —  choose a card reward", width / 2 - r1w2 / 2, height / 2 + 4, 15, new Color(200, 185, 80, 255));
                            if (DrawButton(new Rectangle(width / 2 - 130, height / 2 + 40, 260, 44), "CHOOSE REWARD →", new Color(38, 72, 42, 255), new Color(55, 108, 62, 255)))
                            {
                                profile.Gold += 60;
                                if (activeBattleNode != null)
                                {
                                    activeBattleNode.Completed = true;
                                    completedNodes.Add(activeBattleNode.Id);
                                }
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
                        else
                        {
                            Raylib.DrawRectangle(width / 2 - 270, height / 2 - 80, 540, 190, new Color(22, 14, 14, 240));
                            Raylib.DrawRectangleLinesEx(new Rectangle(width / 2 - 270, height / 2 - 80, 540, 190), 1.5f, new Color(185, 52, 52, 160));
                            int dfw = Raylib.MeasureText("DEFEAT", 56);
                            Raylib.DrawText("DEFEAT", width / 2 - dfw / 2, height / 2 - 68, 56, new Color(215, 60, 60, 255));
                            Raylib.DrawLineEx(new Vector2(width / 2 - 180f, height / 2 - 4f), new Vector2(width / 2 + 180f, height / 2 - 4f), 1f, new Color(155, 48, 48, 160));
                            if (DrawButton(new Rectangle(width / 2 - 115, height / 2 + 22, 230, 44), "RESTART RUN", new Color(88, 28, 28, 255), new Color(128, 42, 42, 255)))
                            {
                                profile.Gold = 150;
                                profile.SkillPoints = 8;
                                completedNodes.Clear();
                                foreach (var cn in campaignNodes) cn.Completed = false;
                                scene = GameScene.TitleScreen;
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
                                profile.ActiveDeck.RemoveAll(c => c.Id == allCards[i].Id);
                            else if (dbCnt < 10)
                                profile.ActiveDeck.Add(allCards[i]);
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
                            scene = GameScene.CampaignMap;
                        }
                    }

                    int skipW2 = Raylib.MeasureText("Skip reward", 13);
                    Raylib.DrawText("Skip reward", width / 2 - skipW2 / 2, height / 2 + rcCH / 2 + 30, 13, new Color(80, 84, 100, 255));
                    if (Raylib.CheckCollisionPointRec(mouse, new Rectangle(width / 2 - skipW2 / 2 - 4, height / 2 + rcCH / 2 + 26, skipW2 + 8, 22))
                        && Raylib.IsMouseButtonPressed(MouseButton.Left))
                        scene = GameScene.CampaignMap;
                    break;
                }
            }

            Raylib.EndDrawing();
        }

        foreach (var s in whooshSounds) Raylib.UnloadSound(s);
        foreach (var s in hitSounds)   Raylib.UnloadSound(s);
        Raylib.CloseAudioDevice();
        Raylib.CloseWindow();
    }
}
