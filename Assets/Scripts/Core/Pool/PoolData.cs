using System.Collections.Generic;
using UnityEngine;

namespace Top
{
    [EditableData]
    public class PoolDataTest
    {
        public string id;

        public string name;

        public string prefabPath;
        public int initialSize = 0;

        public bool expandIfEmpty = true;

        public int maxSize = 0;

        public List<string> tags;

        public int[] tagPriorities;

        public Dictionary<string, int> tagPriorityDict;

    }
}