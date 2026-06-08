using System.Numerics;
using Raylib_cs;
using CardGamePrototype.Core;

namespace CardGamePrototype.Client;

public enum GameScene { TitleScreen, ModeSelect, CampaignMap, BattleView, MarketShop, DeckBuilder }
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
}

public class PlayerProfile
{
    public int Gold { get; set; } = 150;
    public int SkillPoints { get; set; } = 8;
    public List<CardDefinition> TotalCollection { get; set; } = new();
    public List<CardDefinition> ActiveDeck { get; set; } = new();
    public SkillTreeManager SkillTree { get; set; } = new();

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
        "Ignite"    => new Color(235, 95, 15, 255),
        "Firebolt"  => new Color(255, 145, 35, 255),
        "Frost Nova" or "FrostNova" => new Color(65, 175, 240, 255),
        "Push"      => new Color(155, 75, 215, 255),
        "Bio Spore" or "BioSpore"   => new Color(45, 200, 65, 255),
        _           => new Color(140, 145, 165, 255)
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

        for (int i = 0; i < 3; i++) profile.ActiveDeck.Add(CardLibrary.Ignite());
        for (int i = 0; i < 3; i++) profile.ActiveDeck.Add(CardLibrary.Firebolt());
        for (int i = 0; i < 2; i++) profile.ActiveDeck.Add(CardLibrary.Push());
        for (int i = 0; i < 2; i++) profile.ActiveDeck.Add(CardLibrary.FrostNova());

        profile.TotalCollection.AddRange(profile.ActiveDeck);

        List<CampaignNode> campaignNodes = new List<CampaignNode>
        {
            new CampaignNode { Id = 0, Name = "Sector Delta Core", Type = NodeType.CombatMinion, EnemyHp = 35, EnemyDefaultPosition = 1 },
            new CampaignNode { Id = 1, Name = "Outer Perimeter Gate", Type = NodeType.CombatMinion, EnemyHp = 45, EnemyDefaultPosition = 4 },
            new CampaignNode { Id = 2, Name = "Arch-Executioner Frame", Type = NodeType.CombatElite, EnemyHp = 65, EnemyDefaultPosition = 2 },
            new CampaignNode { Id = 3, Name = "The Catalyst Singularity", Type = NodeType.CombatBoss, EnemyHp = 100, EnemyDefaultPosition = 3 }
        };
        int activeNodeIndex = 0;

        BattleService battleService = new BattleService();
        CampaignNode? activeBattleNode = null;

        List<CardDefinition> shopInventory = new List<CardDefinition> { CardLibrary.Ignite(), CardLibrary.Push(), CardLibrary.BioSpore() };
        int cardShopCost = 45;

        Raylib.InitWindow(1600, 900, "Catalyst Architecture");
        Raylib.SetWindowState(ConfigFlags.ResizableWindow);
        Raylib.SetTargetFPS(60);

        bool dragging = false;
        int draggingIndex = -1;
        Vector2 dragOffset = Vector2.Zero;
        bool isDraggingFromCollectionPool = false; 

        while (!Raylib.WindowShouldClose())
        {
            int width = Raylib.GetScreenWidth();
            int height = Raylib.GetScreenHeight();
            Vector2 mouse = Raylib.GetMousePosition();

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

                    for (int i = 0; i < campaignNodes.Count; i++)
                    {
                        var node = campaignNodes[i];
                        int nodeX = 200 + i * (width - 400) / (campaignNodes.Count - 1);
                        int nodeY = height / 2;

                        if (i < campaignNodes.Count - 1)
                        {
                            int nextX = 200 + (i + 1) * (width - 400) / (campaignNodes.Count - 1);
                            Color lineC = campaignNodes[i].Completed ? new Color(45, 130, 65, 220) : new Color(40, 44, 56, 255);
                            Raylib.DrawLineEx(new Vector2(nodeX, nodeY), new Vector2(nextX, nodeY), 3f, lineC);
                        }

                        Color nodeBase = node.Completed   ? new Color(28, 80, 42, 255)
                                       : i == activeNodeIndex ? (node.Type == NodeType.CombatBoss ? new Color(95, 28, 28, 255) : new Color(85, 42, 18, 255))
                                       : new Color(24, 26, 34, 255);
                        Color nodeHov  = node.Completed   ? new Color(38, 105, 55, 255)
                                       : i == activeNodeIndex ? (node.Type == NodeType.CombatBoss ? new Color(135, 38, 38, 255) : new Color(120, 60, 22, 255))
                                       : new Color(34, 36, 46, 255);
                        Color nodeBorder = node.Completed ? new Color(55, 185, 80, 200)
                                         : i == activeNodeIndex ? (node.Type == NodeType.CombatBoss ? new Color(210, 55, 55, 200) : new Color(218, 130, 40, 200))
                                         : new Color(48, 52, 66, 200);

                        Rectangle nodeBox = new(nodeX - 85, nodeY - 55, 170, 110);
                        bool nodeHovered = Raylib.CheckCollisionPointRec(mouse, nodeBox) && i == activeNodeIndex;
                        Raylib.DrawRectangleRounded(nodeBox, 0.1f, 6, nodeHovered ? nodeHov : nodeBase);
                        Raylib.DrawLineEx(new Vector2(nodeBox.X + 6, nodeBox.Y + 1.5f), new Vector2(nodeBox.X + nodeBox.Width - 6, nodeBox.Y + 1.5f), 1f, new Color(255, 255, 255, 30));
                        Raylib.DrawRectangleRoundedLinesEx(nodeBox, 0.1f, 6, 1.5f, nodeBorder);

                        // Type label
                        string typeTag = node.Type switch { NodeType.CombatElite => "ELITE", NodeType.CombatBoss => "BOSS", _ => "SECTOR" };
                        Color typeCol  = node.Type switch { NodeType.CombatElite => new Color(215, 168, 38, 255), NodeType.CombatBoss => new Color(210, 55, 55, 255), _ => new Color(120, 175, 125, 255) };
                        int ttw = Raylib.MeasureText(typeTag, 11);
                        Raylib.DrawText(typeTag, nodeX - ttw / 2, nodeY - 48, 11, typeCol);

                        // Node name
                        int nnw = Raylib.MeasureText(node.Name, 12);
                        // Truncate long names so they fit
                        string displayName = nnw > 152 ? node.Name[..14] + "…" : node.Name;
                        int dnw = Raylib.MeasureText(displayName, 12);
                        Raylib.DrawText(displayName, nodeX - dnw / 2, nodeY - 22, 12, node.Completed ? new Color(145, 210, 155, 255) : Color.White);

                        // HP info
                        string hpStr = $"HP  {node.EnemyHp}";
                        int hpw = Raylib.MeasureText(hpStr, 11);
                        Raylib.DrawText(hpStr, nodeX - hpw / 2, nodeY, 11, new Color(190, 100, 100, 255));

                        // Tier label / completed check
                        if (node.Completed)
                        {
                            int ckw = Raylib.MeasureText("CLEARED", 11);
                            Raylib.DrawText("CLEARED", nodeX - ckw / 2, nodeY + 20, 11, new Color(75, 200, 95, 255));
                        }
                        else
                        {
                            string tierStr = $"Tier {i + 1}";
                            int tw = Raylib.MeasureText(tierStr, 11);
                            Raylib.DrawText(tierStr, nodeX - tw / 2, nodeY + 20, 11, new Color(95, 100, 120, 255));
                        }

                        if (nodeHovered && Raylib.IsMouseButtonPressed(MouseButton.Left) && i == activeNodeIndex)
                        {
                            activeBattleNode = node;
                            battleService.NewBattle();
                            
                            battleService.State.DrawPile.Clear();
                            battleService.State.Hand.Clear();
                            battleService.State.BurnPile.Clear();
                            
                            // HP Bonus logic hooked up to white branch
                            int finalMaxHp = 50 + profile.MaxHpBonus;
                            battleService.State.Player.MaxHp = finalMaxHp; 
                            battleService.State.Player.Hp = finalMaxHp;
                            
                            battleService.State.Enemy.MaxHp = node.EnemyHp;
                            battleService.State.Enemy.Hp = node.EnemyHp;
                            battleService.State.Enemy.Position = node.EnemyDefaultPosition;

                            List<CardDefinition> gameDeckCopy = new List<CardDefinition>(profile.ActiveDeck);
                            for (int k = gameDeckCopy.Count - 1; k > 0; k--)
                            {
                                int idx = battleService.State.Rng.Next(k + 1);
                                var hold = gameDeckCopy[k];
                                gameDeckCopy[k] = gameDeckCopy[idx];
                                gameDeckCopy[idx] = hold;
                            }
                            battleService.State.DrawPile.AddRange(gameDeckCopy);
                            
                            // Starting stats boosted by Skill Tree
                            battleService.State.Player.Energy = 5 + profile.MaxEnergyBonus;
                            int cardsToDraw = 5 + profile.StartCardsBonus;
                            
                            while(battleService.State.Hand.Count < cardsToDraw && battleService.State.DrawPile.Count > 0)
                            {
                                var top = battleService.State.DrawPile[0];
                                battleService.State.DrawPile.RemoveAt(0);
                                battleService.State.Hand.Add(top);
                            }

                            scene = GameScene.BattleView;
                        }
                    }
                    break;

                case GameScene.BattleView:
                    var bs = battleService.State;

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
                        Raylib.DrawLine(width - SW, 80, width, 80, new Color(35, 40, 52, 255));

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
                                if (Raylib.CheckCollisionPointRec(mouse, dz)) { battleService.PlaceCard(draggingIndex, i); break; }
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
                        // === FANTASY: Full-screen Slay-the-Spire-inspired layout ===
                        Color fpText  = new Color(212, 195, 162, 255);
                        Color fpPanel = new Color(34, 24, 14, 240);
                        Color fpBord  = new Color(105, 78, 42, 220);
                        Color fpAcct  = new Color(188, 148, 65, 255);

                        bool showFan = mouse.Y > height * 0.70f || dragging;

                        // Header (0-54)
                        Raylib.DrawRectangle(0, 0, width, 54, new Color(22, 15, 8, 255));
                        Raylib.DrawLine(0, 54, width, 54, new Color(105, 78, 42, 180));
                        DrawBar(12, 14, 200, 10, (float)bs.Player.Hp / Math.Max(bs.Player.MaxHp, 1), new Color(60, 200, 80, 255), new Color(22, 30, 18, 255));
                        int hpFW = Raylib.MeasureText($"{bs.Player.Hp}/{bs.Player.MaxHp}", 12);
                        Raylib.DrawText($"{bs.Player.Hp}/{bs.Player.MaxHp}", 12 + 100 - hpFW / 2, 28, 12, fpText);
                        Raylib.DrawText($"NRG {bs.Player.Energy}", 220, 14, 13, new Color(80, 185, 235, 255));
                        Raylib.DrawText($"DIS {bs.BurnPile.Count}", 220, 32, 12, new Color(205, 140, 48, 255));
                        bool isPlayerTurnFP = bs.Phase == TurnPhase.PlayerTurn;
                        string phaseLblFP = isPlayerTurnFP ? "YOUR TURN" : "ENEMY TURN";
                        Color phaseColFP = isPlayerTurnFP ? new Color(88, 200, 110, 255) : new Color(210, 80, 68, 255);
                        int phWFP = Raylib.MeasureText(phaseLblFP, 13);
                        Raylib.DrawText(phaseLblFP, width / 2 - phWFP / 2, 20, 13, phaseColFP);
                        if (isPlayerTurnFP && DrawButton(new Rectangle(width - 178, 8, 168, 38), "END TURN", new Color(55, 36, 18, 255), new Color(88, 60, 28, 255)))
                            battleService.EndTurn();

                        // Enemy panel (58 to height*0.25)
                        int epBot = (int)(height * 0.25f);
                        Raylib.DrawRectangleRounded(new Rectangle(width / 2 - 300, 58, 600, epBot - 64), 0.08f, 6, fpPanel);
                        Raylib.DrawRectangleRoundedLinesEx(new Rectangle(width / 2 - 300, 58, 600, epBot - 64), 0.08f, 6, 1.5f, fpBord);
                        string eName = activeBattleNode?.Name ?? "Enemy";
                        int eNW = Raylib.MeasureText(eName, 16);
                        Raylib.DrawText(eName, width / 2 - eNW / 2, 68, 16, new Color(200, 85, 75, 255));
                        DrawBar(width / 2 - 200, 92, 400, 10, (float)bs.Enemy.Hp / Math.Max(bs.Enemy.MaxHp, 1), new Color(200, 55, 55, 255), new Color(28, 22, 18, 255));
                        int eHpW = Raylib.MeasureText($"{bs.Enemy.Hp} / {bs.Enemy.MaxHp}", 12);
                        Raylib.DrawText($"{bs.Enemy.Hp} / {bs.Enemy.MaxHp}", width / 2 - eHpW / 2, 106, 12, fpText);
                        var laneSlot = bs.PlayerBoard[Math.Clamp(bs.Enemy.Position, 0, bs.PlayerBoard.Count - 1)];
                        string intentStr = laneSlot.IsOccupied
                            ? $"INTENT: ATTACK  ×{8 + bs.EnemyTurnCount}"
                            : $"INTENT: ADVANCE → Lane {bs.Enemy.Position}";
                        int intentW = Raylib.MeasureText(intentStr, 13);
                        Raylib.DrawText(intentStr, width / 2 - intentW / 2, epBot - 44, 13, laneSlot.IsOccupied ? new Color(215, 80, 65, 255) : new Color(225, 185, 50, 255));
                        int eFire = bs.Enemy.ActiveElements.GetStacks(ElementType.Fire);
                        int eFrost = bs.Enemy.ActiveElements.GetStacks(ElementType.Frost);
                        if (eFire  > 0) Raylib.DrawText($"Fire {eFire}",  width / 2 - 120, epBot - 26, 12, new Color(225, 115, 38, 255));
                        if (eFrost > 0) Raylib.DrawText($"Frost {eFrost}", width / 2 + 50,  epBot - 26, 12, new Color(88, 185, 235, 255));
                        int laneHintW = Raylib.MeasureText($"Lane {bs.Enemy.Position}", 12);
                        Raylib.DrawText($"Lane {bs.Enemy.Position}", width / 2 - laneHintW / 2, epBot - 8, 12, new Color(155, 138, 102, 255));

                        // Full-width grid
                        int gridTop = epBot + 4;
                        int gridBot = showFan ? (int)(height * 0.68f) : height - 40;
                        int gTL = width / 12, gTR = width - width / 12;
                        int gBL = 8, gBR = width - 8;

                        Raylib.DrawRectangle(0, gridTop, width, gridBot - gridTop, new Color(18, 12, 7, 255));
                        for (int xx = 0; xx < width; xx += 80) Raylib.DrawLine(xx, gridTop, xx, gridBot, new Color(30, 22, 14, 255));
                        for (int yy = gridTop; yy < gridBot; yy += 52) Raylib.DrawLine(0, yy, width, yy, new Color(28, 20, 12, 255));
                        Raylib.DrawLineEx(new Vector2(gTL, gridTop), new Vector2(gTR, gridTop), 2, new Color(90, 68, 38, 200));
                        Raylib.DrawLineEx(new Vector2(gBL, gridBot), new Vector2(gBR, gridBot), 2, new Color(90, 68, 38, 200));

                        for (int i = 0; i <= 5; i++)
                        {
                            float tf2 = (float)i / 5;
                            int topX2 = (int)(gTL + tf2 * (gTR - gTL));
                            int botX2 = (int)(gBL + tf2 * (gBR - gBL));
                            Raylib.DrawLineEx(new Vector2(topX2, gridTop), new Vector2(botX2, gridBot), i == 0 || i == 5 ? 2 : 1, new Color(72, 55, 30, 200));
                        }

                        int gMidY = gridTop + (int)((gridBot - gridTop) * 0.45f);
                        int gMidL = (int)(gTL + 0.45f * (gBL - gTL));
                        int gMidR = (int)(gTR + 0.45f * (gBR - gTR));
                        Raylib.DrawLineEx(new Vector2(gMidL, gMidY), new Vector2(gMidR, gMidY), 1, new Color(72, 55, 30, 180));

                        for (int i = 0; i < 5; i++)
                        {
                            float t0 = (float)i / 5, t1 = (float)(i + 1) / 5;
                            int epx0 = (int)(gTL + t0 * (gTR - gTL)) + 4, epx1 = (int)(gTL + t1 * (gTR - gTL)) - 4;
                            int ppx0 = (int)(gBL + t0 * (gBR - gBL)) + 4, ppx1 = (int)(gBL + t1 * (gBR - gBL)) - 4;

                            Rectangle fEZ = new Rectangle(epx0, gridTop + 4, epx1 - epx0, gMidY - gridTop - 8);
                            if (bs.Enemy.Position == i)
                            {
                                float ecx = fEZ.X + fEZ.Width / 2f, ecy = fEZ.Y + fEZ.Height / 2f;
                                float er = Math.Min(fEZ.Width, fEZ.Height) * 0.36f;
                                Raylib.DrawCircleV(new Vector2(ecx, ecy), er, new Color(100, 25, 25, 255));
                                Raylib.DrawCircleLines((int)ecx, (int)ecy, (int)er, new Color(190, 60, 60, 255));
                                Raylib.DrawCircleV(new Vector2(ecx - er * 0.32f, ecy - er * 0.15f), er * 0.15f, new Color(240, 200, 80, 255));
                                Raylib.DrawCircleV(new Vector2(ecx + er * 0.32f, ecy - er * 0.15f), er * 0.15f, new Color(240, 200, 80, 255));
                                Raylib.DrawLineEx(new Vector2(ecx - er * 0.3f, ecy + er * 0.3f), new Vector2(ecx + er * 0.3f, ecy + er * 0.3f), 2, new Color(240, 200, 80, 180));
                                int ehpWFP = Raylib.MeasureText($"{bs.Enemy.Hp}", 11);
                                Raylib.DrawText($"{bs.Enemy.Hp}", (int)(fEZ.X + (fEZ.Width - ehpWFP) / 2), (int)(fEZ.Y + fEZ.Height - 16), 11, new Color(200, 180, 120, 255));
                            }
                            else
                            {
                                Raylib.DrawRectangleLinesEx(fEZ, 1, new Color(60, 45, 28, 160));
                            }

                            Rectangle fDZ = new Rectangle(ppx0, gMidY + 4, ppx1 - ppx0, gridBot - gMidY - 8);
                            bool isTargFP = dragging && Raylib.CheckCollisionPointRec(mouse, fDZ);
                            Raylib.DrawRectangleLinesEx(fDZ, isTargFP ? 3 : 1, isTargFP ? fpAcct : new Color(72, 55, 30, 200));
                            if (isTargFP) Raylib.DrawRectangleRec(new Rectangle(fDZ.X + 1, fDZ.Y + 1, fDZ.Width - 2, fDZ.Height - 2), new Color(188, 148, 65, 22));

                            if (i < bs.PlayerBoard.Count && bs.PlayerBoard[i].IsOccupied)
                            {
                                var occ = bs.PlayerBoard[i].Occupant!;
                                bool uSel = bs.SelectedBoardSlot == i;
                                DrawSummonedUnit(new Rectangle(fDZ.X + 2, fDZ.Y + 2, fDZ.Width - 4, fDZ.Height - 4), occ.SourceCard.Name, occ.Hp, occ.MaxHp, uSel);
                                int aWFP = Raylib.MeasureText($"ATK {occ.BaseAttack}", 10);
                                Raylib.DrawText($"ATK {occ.BaseAttack}", (int)(fDZ.X + (fDZ.Width - aWFP) / 2), (int)(fDZ.Y + 4), 10, fpAcct);
                            }
                        }

                        // Drop release (Fantasy)
                        if (!Raylib.IsMouseButtonDown(MouseButton.Left) && dragging && !isDraggingFromCollectionPool)
                        {
                            for (int i = 0; i < 5; i++)
                            {
                                float t0 = (float)i / 5, t1 = (float)(i + 1) / 5;
                                int ppx0d = (int)(gBL + t0 * (gBR - gBL)) + 4, ppx1d = (int)(gBL + t1 * (gBR - gBL)) - 4;
                                Rectangle dz2 = new Rectangle(ppx0d, gMidY + 4, ppx1d - ppx0d, gridBot - gMidY - 8);
                                if (Raylib.CheckCollisionPointRec(mouse, dz2)) { battleService.PlaceCard(draggingIndex, i); break; }
                            }
                            dragging = false; draggingIndex = -1;
                        }

                        // Hand fan / pile
                        if (!showFan)
                        {
                            int pileCX = width / 2, pileCY = (int)(height * 0.90f);
                            Rectangle pileR = new Rectangle(pileCX - 60, pileCY - 18, 120, 36);
                            Raylib.DrawRectangleRounded(pileR, 0.3f, 6, fpPanel);
                            Raylib.DrawRectangleRoundedLinesEx(pileR, 0.3f, 6, 1.5f, fpBord);
                            int piW = Raylib.MeasureText($"HAND  ×{bs.Hand.Count}", 13);
                            Raylib.DrawText($"HAND  ×{bs.Hand.Count}", pileCX - piW / 2, pileCY - 9, 13, fpText);
                        }
                        else
                        {
                            Raylib.DrawRectangle(0, (int)(height * 0.70f), width, height - (int)(height * 0.70f), new Color(10, 7, 3, 210));
                            Raylib.DrawLine(0, (int)(height * 0.70f), width, (int)(height * 0.70f), new Color(105, 78, 42, 160));

                            int fanY = (int)(height * 0.84f);
                            int cardW = 110, cardH = 150;
                            int count = bs.Hand.Count;
                            int totalW = count * (cardW + 8) - 8;
                            int startX = width / 2 - totalW / 2;

                            int hovFan = -1;
                            for (int i = 0; i < count; i++)
                            {
                                if (dragging && draggingIndex == i) continue;
                                int cx2 = startX + i * (cardW + 8);
                                float arcDip = MathF.Sin((float)i / Math.Max(count - 1, 1) * MathF.PI) * 10f;
                                Rectangle fanR = new Rectangle(cx2, fanY + arcDip, cardW, cardH);
                                if (Raylib.CheckCollisionPointRec(mouse, fanR)) hovFan = i;
                            }

                            for (int i = 0; i < count; i++)
                            {
                                if (dragging && draggingIndex == i) continue;
                                int cx2 = startX + i * (cardW + 8);
                                float arcDip = MathF.Sin((float)i / Math.Max(count - 1, 1) * MathF.PI) * 10f;
                                float liftY = (hovFan == i) ? -22f : 0f;
                                Rectangle fanR = new Rectangle(cx2, fanY + arcDip + liftY, cardW, cardH);
                                var hc2 = bs.Hand[i];
                                DrawCard(fanR, hc2.Name, hc2.Description, hc2.Cost, new Color(28, 20, 10, 255), hovFan == i ? fpAcct : fpBord);
                                if (Raylib.IsMouseButtonPressed(MouseButton.Left) && Raylib.CheckCollisionPointRec(mouse, fanR) && !dragging)
                                {
                                    dragging = true; draggingIndex = i; isDraggingFromCollectionPool = false;
                                    dragOffset = mouse - new Vector2(fanR.X, fanR.Y);
                                }
                            }

                            // Enlarged preview above hovered fan card
                            if (hovFan >= 0 && !dragging && hovFan < bs.Hand.Count)
                            {
                                int pvW2 = 175, pvH2 = 235;
                                int cx2 = startX + hovFan * (cardW + 8);
                                float arcDip2 = MathF.Sin((float)hovFan / Math.Max(count - 1, 1) * MathF.PI) * 10f;
                                Rectangle pvR2 = new Rectangle(
                                    Math.Clamp(cx2 + cardW / 2 - pvW2 / 2, 4, width - pvW2 - 4),
                                    fanY + arcDip2 - pvH2 - 8, pvW2, pvH2);
                                DrawCard(pvR2, bs.Hand[hovFan].Name, bs.Hand[hovFan].Description, bs.Hand[hovFan].Cost, new Color(28, 20, 10, 255), fpAcct);
                            }
                        }

                        // Fantasy drag ghost
                        if (dragging && draggingIndex >= 0 && draggingIndex < bs.Hand.Count)
                        {
                            var dc = bs.Hand[draggingIndex];
                            DrawCard(new Rectangle(mouse.X - dragOffset.X, mouse.Y - dragOffset.Y, 130, 175), dc.Name, dc.Description, dc.Cost, new Color(44, 32, 16, 230), fpAcct);
                        }
                    }

                    // Win/Lose overlay (both themes)
                    if (bs.Phase == TurnPhase.Finished)
                    {
                        Raylib.DrawRectangle(0, 0, width, height, new Color(8, 9, 13, 215));
                        if (bs.Enemy.IsDead)
                        {
                            Raylib.DrawRectangle(width / 2 - 290, height / 2 - 100, 580, 230, new Color(18, 22, 16, 240));
                            Raylib.DrawRectangleLinesEx(new Rectangle(width / 2 - 290, height / 2 - 100, 580, 230), 1.5f, new Color(72, 185, 88, 160));
                            int vcw = Raylib.MeasureText("VICTORY", 56);
                            Raylib.DrawText("VICTORY", width / 2 - vcw / 2, height / 2 - 88, 56, Color.Gold);
                            Raylib.DrawLineEx(new Vector2(width / 2 - 220f, height / 2 - 24f), new Vector2(width / 2 + 220f, height / 2 - 24f), 1f, new Color(68, 155, 78, 160));
                            int r1w = Raylib.MeasureText("+60 Credits", 17);
                            Raylib.DrawText("+60 Credits", width / 2 - r1w / 2, height / 2 - 10, 17, new Color(200, 185, 80, 255));
                            int r2w = Raylib.MeasureText("Bio Spore added to collection", 14);
                            Raylib.DrawText("Bio Spore added to collection", width / 2 - r2w / 2, height / 2 + 14, 14, new Color(88, 205, 110, 255));
                            if (DrawButton(new Rectangle(width / 2 - 120, height / 2 + 60, 240, 44), "RETURN TO MAP", new Color(38, 72, 42, 255), new Color(55, 108, 62, 255)))
                            {
                                profile.Gold += 60;
                                profile.TotalCollection.Add(CardLibrary.BioSpore());
                                if (activeBattleNode != null)
                                {
                                    activeBattleNode.Completed = true;
                                    if (activeNodeIndex < campaignNodes.Count - 1) activeNodeIndex++;
                                }
                                scene = GameScene.CampaignMap;
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
                                activeNodeIndex = 0;
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

                    // === LEFT HALF: SHOP ===
                    Raylib.DrawText("SCHEMA CARDS", 110, 132, 18, Color.Gold);
                    Raylib.DrawLine(110, 156, 110 + shopInventory.Count * 233 - 18, 156, new Color(90, 75, 25, 110));

                    for (int i = 0; i < shopInventory.Count; i++)
                    {
                        int sx = 110 + i * 233;
                        DrawCard(new Rectangle(sx, 168, 215, 268), shopInventory[i].Name, shopInventory[i].Description, shopInventory[i].Cost, new Color(20, 24, 34, 255), new Color(40, 62, 108, 255));
                        bool canAfford = profile.Gold >= cardShopCost;
                        if (DrawButton(new Rectangle(sx, 448, 215, 38), $"BUY  {cardShopCost} G",
                            canAfford ? new Color(35, 60, 45, 255) : new Color(42, 36, 36, 255),
                            canAfford ? new Color(50, 95, 65, 255) : new Color(58, 42, 42, 255)) && canAfford)
                        {
                            profile.Gold -= cardShopCost;
                            profile.TotalCollection.Add(shopInventory[i]);
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
                    Raylib.DrawRectangle(0, 0, width, 58, new Color(14, 16, 21, 255));
                    Raylib.DrawLine(0, 58, width, 58, new Color(35, 40, 52, 255));
                    Raylib.DrawText("DECK BUILDER", 28, 16, 22, Color.White);
                    Raylib.DrawText($"{profile.ActiveDeck.Count} / 15", width - 115, 16, 20, Color.Yellow);

                    if (DrawButton(new Rectangle(40, 74, 155, 38), "< SAVE & EXIT", new Color(38, 72, 42, 255), new Color(55, 108, 62, 255)))
                        scene = GameScene.CampaignMap;

                    int colWidth = 150;
                    int colHeight = 200;

                    Raylib.DrawText("ACTIVE DECK  —  click to remove", 60, 140, 16, new Color(195, 165, 52, 255));
                    for (int i = 0; i < profile.ActiveDeck.Count; i++)
                    {
                        Rectangle cardBox = new Rectangle(60 + i * (colWidth + 12), 190, colWidth, colHeight);
                        DrawCard(cardBox, profile.ActiveDeck[i].Name, profile.ActiveDeck[i].Description, profile.ActiveDeck[i].Cost, new Color(18, 22, 32, 255), new Color(38, 105, 52, 255));

                        if (Raylib.IsMouseButtonPressed(MouseButton.Left) && Raylib.CheckCollisionPointRec(mouse, cardBox) && !dragging)
                        {
                            profile.ActiveDeck.RemoveAt(i);
                            break;
                        }
                    }

                    Raylib.DrawLine(40, height / 2 + 38, width - 40, height / 2 + 38, new Color(35, 40, 52, 255));
                    Raylib.DrawText("COLLECTION  —  drag to add to deck", 60, height / 2 + 46, 16, new Color(72, 155, 195, 255));
                    int itemsPerRow = (width - 120) / (colWidth + 12);
                    for (int i = 0; i < profile.TotalCollection.Count; i++)
                    {
                        int row = i / itemsPerRow;
                        int col = i % itemsPerRow;
                        Rectangle cardBox = new Rectangle(60 + col * (colWidth + 12), (height / 2 + 90) + row * (colHeight + 15), colWidth, colHeight);

                        if (dragging && isDraggingFromCollectionPool && draggingIndex == i) continue;

                        DrawCard(cardBox, profile.TotalCollection[i].Name, profile.TotalCollection[i].Description, profile.TotalCollection[i].Cost, new Color(20, 24, 36, 255), new Color(48, 52, 70, 255));

                        if (Raylib.IsMouseButtonPressed(MouseButton.Left) && Raylib.CheckCollisionPointRec(mouse, cardBox) && !dragging)
                        {
                            dragging = true;
                            draggingIndex = i;
                            isDraggingFromCollectionPool = true;
                            dragOffset = mouse - new Vector2(cardBox.X, cardBox.Y);
                        }
                    }

                    if (dragging && Raylib.IsMouseButtonReleased(MouseButton.Left))
                    {
                        Rectangle dropZone = new Rectangle(40, 140, width - 80, 270);
                        if (Raylib.CheckCollisionPointRec(mouse, dropZone) && isDraggingFromCollectionPool && profile.ActiveDeck.Count < 15)
                        {
                            profile.ActiveDeck.Add(profile.TotalCollection[draggingIndex]);
                        }
                        dragging = false;
                        draggingIndex = -1;
                        isDraggingFromCollectionPool = false;
                    }

                    if (dragging && isDraggingFromCollectionPool && draggingIndex < profile.TotalCollection.Count)
                    {
                        var floatCard = profile.TotalCollection[draggingIndex];
                        DrawCard(new Rectangle(mouse.X - dragOffset.X, mouse.Y - dragOffset.Y, colWidth, colHeight), floatCard.Name, floatCard.Description, floatCard.Cost, new Color(255, 240, 170, 230), Color.Gold);
                    }
                    break;
            }

            Raylib.EndDrawing();
        }

        Raylib.CloseWindow();
    }
}
