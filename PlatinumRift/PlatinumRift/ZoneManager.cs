using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PlatinumRift
{
    public class ZoneManager
    {
        public Zone MyBase { get; set; }
        public Zone OpponentBase { get; set; }
        public List<Zone> Zones { get; }

        public ZoneManager()
        {
            Zones = new List<Zone>();
        }

        public Zone GetById(int id)
        {
            foreach(Zone zone in Zones)
            {
                if(zone.Id == id)
                {
                    return zone;
                }
            }
            return null;
        }

        public void Add(Zone zone)
        {
            if(!Zones.Contains(zone))
            {
                Zones.Add(zone);
            }
        }
    }
}
