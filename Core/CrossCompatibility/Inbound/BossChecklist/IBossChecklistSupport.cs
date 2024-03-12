using System.Collections.Generic;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace YouBoss.Core.CrossCompatibility.Inbound
{
    public interface IBossChecklistSupport
    {
        public bool IsMiniboss
        {
            get;
        }

        public string ChecklistEntryName
        {
            get;
        }

        // Here are the boss values from vanilla and Calamity, to give a bit of context:
        // King Slime = 1
        // Eye of Cthulhu = 2
        // Eater of Worlds and Brain of Cthulhu = 3
        // Queen Bee = 4
        // Skeletron = 5
        // Deerclops = 6
        // Wall of Flesh = 7
        // Queen Slime = 8
        // The Twins = 9
        // The Destroyer = 10
        // Skeletron Prime = 11
        // Plantera = 12
        // Golem = 13
        // Duke Fishron = 14
        // Empress of Light = 15
        // Betsy = 16 (I don't know why this is counted by Boss Checklist as a proper boss with its own tier stepup but it is)
        // Lunatic Cultist = 17
        // Moon Lord = 18
        public float ProgressionValue
        {
            get;
        }

        public bool IsDefeated
        {
            get;
        }

        public List<int> Collectibles
        {
            get;
        }

        public int? SpawnItem => null;

        public bool UsesCustomPortraitDrawing => false;

        public void DrawCustomPortrait(SpriteBatch spriteBatch, Rectangle area, Color color) { }
    }
}
