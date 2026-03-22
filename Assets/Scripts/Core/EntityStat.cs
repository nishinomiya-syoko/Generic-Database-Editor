using UnityEngine;
using System.Collections;
using System.Collections.Generic;

namespace Top
{
    public class EntityStat
    {
        public Dictionary<string, int> stats = new Dictionary<string, int>();
        public int GetStat(string statName)
        {
            return stats.ContainsKey(statName) ? stats[statName] : 0;
        }
        public void SetStat(string statName, int value)
        {
            if (stats.ContainsKey(statName))
            {
                stats[statName] = value;
            }
            else
            {
                stats.Add(statName, value);
            }
        }
    }
}