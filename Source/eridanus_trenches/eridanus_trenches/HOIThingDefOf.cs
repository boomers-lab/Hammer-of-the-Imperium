using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using RimWorld;
using Verse;

namespace eridanus_trenches
{
    [DefOf]
    public static class HOIThingDefOf
    {
        public static ThingDef GW_Trenches;

        static HOIThingDefOf()
        {
            DefOfHelper.EnsureInitializedInCtor(typeof(ThingDefOf));
        }
    }
}
