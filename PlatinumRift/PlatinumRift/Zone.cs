using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PlatinumRift
{
    public class Zone
    {
        public int Id { get; }
        public int PlatinumSource { get; set; }
        public List<Zone> Links { get; }
        public bool IsVisible { get; set; }
        public enum Owner
        {
            ME,
            OPPONENT,
            NEUTRAL
        };
        public Owner ZoneOwner { get; set; }
        public int MyPodCount { get; set; }
        public int OpponentPodCount { get; set; }

        public Zone(int id, int platinumSource)
        {
            Id = id;
            PlatinumSource = platinumSource;
            Links = new List<Zone>();
            MyPodCount = 0;
            OpponentPodCount = 0;
        }

        public void AddLink(Zone zone)
        {
            if(!Links.Contains(zone))
            {
                Links.Add(zone);
                zone.Links.Add(this);
            }
        }

        public override bool Equals(object obj)
        {
            return this.Equals(obj as Zone);
        }

        public bool Equals(Zone zone)
        {
            if(Object.ReferenceEquals(zone, null))
            {
                return false;
            }

            if(Object.ReferenceEquals(this, zone))
            {
                return true;
            }

            if(this.GetType() != zone.GetType())
            {
                return false;
            }

            return Id == zone.Id;
        }

        public override int GetHashCode()
        {
            return Id * 0x00010000;
        }

        public static bool operator ==(Zone leftZone, Zone rightZone)
        {
            if(Object.ReferenceEquals(leftZone, null))
            {
                if(Object.ReferenceEquals(rightZone, null))
                {
                    return true;
                }
                return false;
            }
            return leftZone.Equals(rightZone);
        }

        public static bool operator !=(Zone leftZone, Zone rightZone)
        {
            return !(leftZone == rightZone);
        }

        public override string ToString()
        {
            return "" + Id;
        }
    }
}
