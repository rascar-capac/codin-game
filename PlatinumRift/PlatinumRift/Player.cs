using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PlatinumRift
{
    public class Player
    {
        // decision parameters
        private static readonly int OPPONENT_ZONE_VALUE = 1;
        private static readonly int MIN_DEFENDER_COUNT = 3;
        private static readonly int MIN_EXPLORER_COUNT = 7;
        // used to determine which exploration factor to use
        private static readonly double PODS_BY_ZONE_PROPORTION = 0.2;
        // used to compute the explorer count threshold
        private static readonly double FACTOR_FOR_HEAVY_EXPLORATION = 0.1;
        private static readonly double FACTOR_FOR_LIGHT_EXPLORATION = 0.05;

        public int MyId { get; }
        public int MyPlatinum { get; set; }
        public ZoneManager ZoneManager { get; }
        public enum DestinationType
        {
            MY_BASE,
            OPPONENT_BASE,
            NOT_OWNED_ZONE
        }
        public int DefenderCount { get; set; }
        public List<Zone> CurrentZonesInExploration { get; }
        public List<Zone> NextZonesInExploration { get; }
        public bool IsAtFirstTurn { get; set; }

        public Player(int myId)
        {
            MyId = myId;
            ZoneManager = new ZoneManager();
            DefenderCount = 0;
            CurrentZonesInExploration = new List<Zone>();
            NextZonesInExploration = new List<Zone>();
            IsAtFirstTurn = true;
        }

        // adds an instruction for one of my pods
        public void ComputeInstruction(Zone zone, List<Instruction> instructions)
        {
            // 0 means that there are invaders but 2 zones away
            int invaderCount = getCloseInvaderCount();
            // if on base and enough pods on it, the pod attacks the invaders
            //        !!! CODE JAVA !!!
            //        if(invaderCount >= 0 && zone.Equals(ZoneManager.getMyBase())) {
            //            boolean hasInstruction = false;
            //            Iterator<Zone> linkedZoneIt = zone.Links.iterator();
            //            do {
            //                Zone linkedZone = linkedZoneIt.next();
            //                if(linkedZone.OpponentPodCount > 0) {
            //                    int podCount = 0;
            //                    for(Instruction zoneInstruction : zoneInstructions) {
            //                        if(zoneInstruction.getDestination().Equals(linkedZone)) {
            //                            podCount += zoneInstruction.getPodCount();
            //                        }
            //                    }
            //                    // décider combien de pods envoyer
            //                    Console.Error.WriteLine("attacking invaders");
            //                    hasInstruction = true;
            //                }
            //            } while(!hasInstruction && linkedZoneIt.hasNext());
            //        }

            // if my base is under attack, the pod gets back to it (if not already enough defenders)
            if(invaderCount >= 0 && DefenderCount < MIN_DEFENDER_COUNT + invaderCount)
            {
                Console.Error.WriteLine("getting back to base");
                DefenderCount++;
                instructions.Add(new Instruction(
                        1,
                        zone,
                        FindPathTo(zone, DestinationType.MY_BASE, instructions)
                ));
            }

            // if the pod is in exploration, it explores optimally
            else if(CurrentZonesInExploration.Contains(zone))
            {
                Zone destination = FindBestExploration(zone, instructions);
                if(!NextZonesInExploration.Contains(destination))
                {
                    NextZonesInExploration.Add(destination);
                }
                instructions.Add(new Instruction(
                        1,
                        zone,
                        destination
                ));
            }

            // otherwise, it moves towards the opponent base
            else if(zone != ZoneManager.OpponentBase)
            {
                Console.Error.WriteLine("attacking opponent base");
                instructions.Add(new Instruction(
                        1,
                        zone,
                        FindPathTo(zone, DestinationType.OPPONENT_BASE, instructions)
                ));
            }
        }

        public int getCloseInvaderCount()
        {
            int invaderCount = 0;
            bool isUnderAttack = false;
            foreach(Zone linkedZone in ZoneManager.MyBase.Links)
            {
                invaderCount += linkedZone.OpponentPodCount;
                foreach(Zone linkedZone2 in linkedZone.Links)
                {
                    if(linkedZone2.OpponentPodCount > 0)
                    {
                        isUnderAttack = true;
                    }
                }
            }
            if(invaderCount == 0 && !isUnderAttack)
            {
                return -1;
            }
            else
            {
                return invaderCount;
            }
        }

        public Zone FindBestExploration(Zone zone, List<Instruction> instructions)
        {
            List<Zone> links = zone.Links;
            int maxScore = 0;
            Zone maxScoreZone = null;
            bool hasOnlyOwnedZones = true;

            foreach(Zone linkedZone in links)
            {
                // if next to opponent base, the pod attacks it
                if(linkedZone == ZoneManager.OpponentBase)
                {
                    Console.Error.WriteLine("exploring but near opponent base");
                    return linkedZone;
                }

                bool isBeingExplored = false;
                for(int i = 0 ; i < instructions.Count && !isBeingExplored ; i++)
                {
                    if(instructions[i].Destination ==linkedZone)
                    {
                        isBeingExplored = true;
                    }
                }

                // computes zone score if not already mine
                if(linkedZone.ZoneOwner != Zone.Owner.ME && !isBeingExplored)
                {
                    hasOnlyOwnedZones = false;
                    int score = 1;
                    if(linkedZone.ZoneOwner == Zone.Owner.OPPONENT)
                    {
                        score += OPPONENT_ZONE_VALUE;
                    }
                    score += linkedZone.PlatinumSource;

                    if(score > maxScore || (score == maxScore && linkedZone.ZoneOwner == Zone.Owner.OPPONENT))
                    {
                        maxScore = score;
                        maxScoreZone = linkedZone;
                    }
                }
            }

            // if surrounded by owned zones, it can be that other pods of the zone have already moved on each of them
            // if not, then it finds a new path to a zone not owned
            if(hasOnlyOwnedZones)
            {
                int minPodCount = Int32.MaxValue;
                Zone minPodCountZone = null;
                foreach(Zone linkedZone in zone.Links)
                {
                    int explorerCount = 0;
                    foreach(Instruction instruction in instructions)
                    {
                        if(instruction.Origin == zone &&
                                instruction.Destination == linkedZone)
                        {
                            explorerCount += instruction.PodCount;
                        }
                    }
                    if(explorerCount > 0 && explorerCount < minPodCount)
                    {
                        minPodCount = explorerCount;
                        minPodCountZone = linkedZone;
                    }
                }
                if(minPodCountZone == null)
                {
                    Console.Error.WriteLine("exploring but surrounded by owned zones");
                    return FindPathTo(zone, DestinationType.NOT_OWNED_ZONE, instructions);
                }
                else
                {
                    Console.Error.WriteLine("exploring on " + minPodCountZone.Id);
                    return minPodCountZone;
                }
            }

            Console.Error.WriteLine("exploring on " + maxScoreZone.Id);

            return maxScoreZone;
        }

        // returns next zone of path towards given destination
        public Zone FindPathTo(Zone zone, DestinationType destination, List<Instruction> instructions)
        {
            Dictionary<Zone, Zone> predecessors = new Dictionary<Zone, Zone>();
            List<Zone> queue = new List<Zone>();
            Zone currentZone;

            // breadth first search
            bool isFound = false;
            predecessors.Add(zone, zone);
            queue.Add(zone);
            do
            {
                currentZone = queue[0];
                queue.RemoveAt(0);
                for(int i = 0 ; i < currentZone.Links.Count && !isFound ; i++)
                {
                    Zone linkedZone = currentZone.Links[i];
                    if(!predecessors.ContainsKey(linkedZone))
                    {
                        predecessors.Add(linkedZone, currentZone);
                        queue.Add(linkedZone);

                        switch(destination)
                        {
                            case DestinationType.MY_BASE:
                                isFound = linkedZone == ZoneManager.MyBase;
                                break;
                            case DestinationType.OPPONENT_BASE:
                                isFound = linkedZone == ZoneManager.OpponentBase;
                                break;
                            case DestinationType.NOT_OWNED_ZONE:
                                bool isBeingExplored = false;
                                for(int j = 0 ; j < instructions.Count && !isBeingExplored ; j++)
                                {
                                    if(instructions[j].Destination == linkedZone)
                                    {
                                        isBeingExplored = true;
                                    }
                                }
                                isFound = linkedZone.ZoneOwner != Zone.Owner.ME && !isBeingExplored && linkedZone.IsVisible;
                                break;
                        }
                        if(isFound)
                        {
                            currentZone = linkedZone;
                        }
                    }
                }
            } while(!isFound && queue.Any());

            // travels back to origin through predecessors and returns the zone just before it
            Zone path;
            do
            {
                path = currentZone;
                currentZone = predecessors[currentZone];
            } while(currentZone != zone);
            return path;
        }

        static void Main(string[] args)
        {
            string[] inputs;
            inputs = Console.ReadLine().Split(' ');
            int playerCount = int.Parse(inputs[0]); // the amount of players (always 2)
            int myId = int.Parse(inputs[1]); // my player ID (0 or 1)
            Player player = new Player(myId);

            int zoneCount = int.Parse(inputs[2]); // the amount of zones on the map
            int linkCount = int.Parse(inputs[3]); // the amount of links between all zones
            for(int i = 0 ; i < zoneCount ; i++)
            {
                inputs = Console.ReadLine().Split(' ');
                int zoneId = int.Parse(inputs[0]); // this zone's ID (between 0 and zoneCount-1)
                int platinumSource = int.Parse(inputs[1]); // because of the fog, will always be 0
                Zone zone = new Zone(zoneId, platinumSource);
                player.ZoneManager.Add(zone);
            }
            for(int i = 0 ; i < linkCount ; i++)
            {
                inputs = Console.ReadLine().Split(' ');
                int zone1 = int.Parse(inputs[0]);
                int zone2 = int.Parse(inputs[1]);
                player.ZoneManager.GetById(zone1).AddLink(player.ZoneManager.GetById(zone2)); // Adds the other zone to both zones’ links list
            }

            // game loop
            while(true)
            {
                player.MyPlatinum = int.Parse(Console.ReadLine()); // my available Platinum
                int myGlobalPodCount = 0;
                for(int i = 0 ; i < zoneCount ; i++)
                {
                    inputs = Console.ReadLine().Split(' ');
                    int zId = int.Parse(inputs[0]); // this zone's ID
                    Zone zone = player.ZoneManager.GetById(zId);

                    int ownerId = int.Parse(inputs[1]); // the player who owns this zone (-1 otherwise)
                    if(ownerId == player.MyId)
                    {
                        zone.ZoneOwner = Zone.Owner.ME;
                    }
                    else
                    {
                        zone.ZoneOwner = (ownerId == -1 ? Zone.Owner.NEUTRAL : Zone.Owner.OPPONENT);
                    }

                    int podsP0 = int.Parse(inputs[2]); // player 0's PODs on this zone
                    int podsP1 = int.Parse(inputs[3]); // player 1's PODs on this zone
                    if(player.MyId == 0)
                    {
                        zone.MyPodCount = podsP0;
                        zone.OpponentPodCount = podsP1;
                    }
                    else
                    {
                        zone.OpponentPodCount = podsP0;
                        zone.MyPodCount = podsP1;
                    }

                    int visibility = int.Parse(inputs[4]); // 1 if one of my units can see this tile, else 0
                    zone.IsVisible = (visibility == 1);
                    // finds my base’s id and that of my opponent, only for the first turn
                    if(player.IsAtFirstTurn)
                    {
                        if(zone.IsVisible)
                        {
                            if(zone.MyPodCount != 0)
                            {
                                player.ZoneManager.MyBase = zone;
                            }
                            else if(zone.OpponentPodCount != 0)
                            {
                                player.ZoneManager.OpponentBase =zone;
                            }
                        }
                    }

                    int platinum = int.Parse(inputs[5]); // the amount of Platinum this zone can provide (0 if hidden by fog)
                    if(visibility == 1)
                    {
                        zone.PlatinumSource = platinum;
                    }

                    myGlobalPodCount += zone.MyPodCount;
                }

                // reorders zones by proximity with my base (with breadth first search)
                if(player.IsAtFirstTurn)
                {
                    player.ZoneManager.Zones.Clear();
                    List<Zone> queue = new List<Zone>();
                    player.ZoneManager.Add(player.ZoneManager.MyBase);
                    queue.Add(player.ZoneManager.MyBase);
                    do
                    {
                        foreach(Zone linkedZone in queue[0].Links)
                        {
                            if(!player.ZoneManager.Zones.Contains(linkedZone))
                            {
                                player.ZoneManager.Add(linkedZone);
                                queue.Add(linkedZone);
                            }
                        }
                        queue.RemoveAt(0);
                    } while(queue.Any());
                }

                // computes the exploration threshold according to the ratio of my pods by zone
                bool mustExploreALot = myGlobalPodCount < zoneCount * PODS_BY_ZONE_PROPORTION;
                int explorerCountThreshold = (int)(
                        zoneCount *
                        (mustExploreALot ? FACTOR_FOR_HEAVY_EXPLORATION : FACTOR_FOR_LIGHT_EXPLORATION)
                );
                if(explorerCountThreshold < MIN_EXPLORER_COUNT)
                {
                    explorerCountThreshold = MIN_EXPLORER_COUNT;
                }
                Console.Error.WriteLine(
                        explorerCountThreshold + " exploring pod(s) needed - " +
                        (mustExploreALot ? "heavy" : "light") + " exploration"
                );

                int explorerCount = 0;
                foreach(Zone zoneInExploration in player.CurrentZonesInExploration)
                {
                    explorerCount += zoneInExploration.MyPodCount;
                }
                Console.Error.WriteLine(explorerCount + " exploring pod(s)");

                // generates instructions for every zone with pods
                List<Instruction> instructions = new List<Instruction>();
                foreach(Zone zone in player.ZoneManager.Zones)
                {
                    int podCount = zone.MyPodCount;
                    if(podCount > 0)
                    {
                        Console.Error.WriteLine(zone.Id + " - " + podCount + " :");

                        if(zone == player.ZoneManager.MyBase)
                        {
                            // there must always be at least one pod on my base
                            if(player.MyPlatinum < 20)
                            {
                                podCount--;
                                Console.Error.WriteLine("staying on base");
                            }

                            if(explorerCount < explorerCountThreshold)
                            {
                                player.CurrentZonesInExploration.Add(zone);
                            }
                        }

                        for(int i = 0 ; i < podCount ; i++)
                        {
                            Console.Error.Write(i + 1 + " - ");
                            player.ComputeInstruction(zone, instructions);
                                
                            // limits number of new exploring pods if threshold exceeded
                            if(zone == player.ZoneManager.MyBase && player.CurrentZonesInExploration.Contains(zone))
                            {
                                explorerCount++;
                                if(explorerCount == explorerCountThreshold)
                                {
                                    player.CurrentZonesInExploration.Remove(zone);
                                }
                            }
                        }
                    }
                }

                player.DefenderCount = 0;

                player.CurrentZonesInExploration.Clear();
                player.CurrentZonesInExploration.AddRange(player.NextZonesInExploration);
                player.NextZonesInExploration.Clear();


                String instructionString = "";
                foreach(Instruction instruction in instructions)
                {
                    instructionString += instruction.ToString() + " ";
                }

                Console.WriteLine(instructionString);
                Console.WriteLine("WAIT");

                player.IsAtFirstTurn = false;
            }
        }
    }
}
