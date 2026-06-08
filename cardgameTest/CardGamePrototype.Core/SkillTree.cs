using System.Collections.Generic;

namespace CardGamePrototype.Core
{
    public enum SkillBranch { Origin, Green, Blue, Red, Yellow, White }

    public class SkillNode
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public int Cost { get; set; }
        public bool IsUnlocked { get; set; }
        public int Column { get; set; } // Depth from center
        public SkillBranch Branch { get; set; }
        public List<int> PrerequisiteIds { get; set; } = new List<int>();
    }

    public class SkillTreeManager
    {
        public List<SkillNode> Nodes { get; set; } = new List<SkillNode>();
        public bool DebugMode { get; set; }

        public SkillTreeManager()
        {
            // Center Origin
            Nodes.Add(new SkillNode { Id = 0, Name = "Catalyst Origin", Description = "Awaken the engine.", Cost = 0, IsUnlocked = true, Column = 0, Branch = SkillBranch.Origin });

            // Green Branch (Energy Expansion)
            Nodes.Add(new SkillNode { Id = 1, Name = "Kinetic Battery I", Description = "+1 Max Energy", Cost = 1, Column = 1, Branch = SkillBranch.Green, PrerequisiteIds = { 0 } });
            Nodes.Add(new SkillNode { Id = 2, Name = "Kinetic Battery II", Description = "+1 Max Energy", Cost = 2, Column = 2, Branch = SkillBranch.Green, PrerequisiteIds = { 1 } });
            Nodes.Add(new SkillNode { Id = 3, Name = "Overclock", Description = "+2 Max Energy", Cost = 3, Column = 3, Branch = SkillBranch.Green, PrerequisiteIds = { 2 } });

            // Blue Branch (Draw Mechanics)
            Nodes.Add(new SkillNode { Id = 4, Name = "Optic Sensor I", Description = "+1 Starting Card", Cost = 1, Column = 1, Branch = SkillBranch.Blue, PrerequisiteIds = { 0 } });
            Nodes.Add(new SkillNode { Id = 5, Name = "Optic Sensor II", Description = "+1 Starting Card", Cost = 2, Column = 2, Branch = SkillBranch.Blue, PrerequisiteIds = { 4 } });

            // Red Branch (Offensive Output)
            Nodes.Add(new SkillNode { Id = 6, Name = "Thermic Coils I", Description = "+1 Minion Base DMG", Cost = 1, Column = 1, Branch = SkillBranch.Red, PrerequisiteIds = { 0 } });
            Nodes.Add(new SkillNode { Id = 7, Name = "Thermic Coils II", Description = "+2 Minion Base DMG", Cost = 2, Column = 2, Branch = SkillBranch.Red, PrerequisiteIds = { 6 } });
            Nodes.Add(new SkillNode { Id = 8, Name = "Ignition Flare", Description = "+3 Minion Base DMG", Cost = 3, Column = 4, Branch = SkillBranch.Red, PrerequisiteIds = { 7 } });

            // Yellow Branch (Economy)
            Nodes.Add(new SkillNode { Id = 9, Name = "Gold Processor I", Description = "+15 Bonus Run Gold", Cost = 1, Column = 1, Branch = SkillBranch.Yellow, PrerequisiteIds = { 0 } });
            Nodes.Add(new SkillNode { Id = 10, Name = "Gold Processor II", Description = "+25 Bonus Run Gold", Cost = 2, Column = 2, Branch = SkillBranch.Yellow, PrerequisiteIds = { 9 } });

            // White Branch (Durability)
            Nodes.Add(new SkillNode { Id = 11, Name = "Hull Plating I", Description = "+10 Player Max HP", Cost = 1, Column = 1, Branch = SkillBranch.White, PrerequisiteIds = { 0 } });
            Nodes.Add(new SkillNode { Id = 12, Name = "Hull Plating II", Description = "+15 Player Max HP", Cost = 2, Column = 2, Branch = SkillBranch.White, PrerequisiteIds = { 11 } });
            Nodes.Add(new SkillNode { Id = 13, Name = "Aegis Core", Description = "+25 Player Max HP", Cost = 3, Column = 5, Branch = SkillBranch.White, PrerequisiteIds = { 12 } });
        }

        public bool IsAvailable(int nodeId)
        {
            var node = Nodes.Find(n => n.Id == nodeId);
            if (node == null || node.IsUnlocked) return false;

            foreach (var reqId in node.PrerequisiteIds)
            {
                var reqNode = Nodes.Find(n => n.Id == reqId);
                if (reqNode != null && reqNode.IsUnlocked) return true; 
            }
            return false;
        }

        // QOL Math: Calculates total SP required to purchase a node deep in the tree
        public int GetTotalCostToUnlock(int nodeId)
        {
            var node = Nodes.Find(n => n.Id == nodeId);
            if (node == null || node.IsUnlocked) return 0;

            int total = node.Cost;
            foreach (var reqId in node.PrerequisiteIds)
            {
                total += GetTotalCostToUnlock(reqId);
            }
            return total;
        }

        // QOL Logic: Recursively buys all locked parents if the player has enough SP
        public void UnlockPath(int nodeId, ref int skillPoints)
        {
            var node = Nodes.Find(n => n.Id == nodeId);
            if (node == null || node.IsUnlocked) return;

            foreach (var reqId in node.PrerequisiteIds)
            {
                UnlockPath(reqId, ref skillPoints);
            }

            if (skillPoints >= node.Cost && !node.IsUnlocked)
            {
                skillPoints -= node.Cost;
                node.IsUnlocked = true;
            }
        }
    }
}