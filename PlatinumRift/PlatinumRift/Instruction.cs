using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PlatinumRift
{
    public class Instruction
    {
        public int PodCount { get; }
        public Zone Origin { get; }
        public Zone Destination { get; }

        public Instruction(int podCount, Zone origin, Zone destination)
        {
            PodCount = podCount;
            Origin = origin;
            Destination = destination;
        }

        public override string ToString()
        {
            return PodCount + " " + Origin + " " + Destination;
        }
    }
}
