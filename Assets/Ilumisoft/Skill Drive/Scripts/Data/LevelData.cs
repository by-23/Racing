using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Ilumisoft.SkillDrive.Game
{
    [CreateAssetMenu()]
    public class LevelData : ScriptableObject
    {
        [SerializeField]
        int firstLevelIndex = 0;

        [SerializeField]
        int levelCount = 0;

        public int LevelCount { get => this.levelCount; set => this.levelCount = value; }

        /// <summary>
        /// Returns an array containing the build indices of all levels in ascending order
        /// </summary>
        /// <returns></returns>
        public int[] GetLevelBuildIndeces()
        {
            List<int> result = new List<int>();

            for(int i=0; i<levelCount;i++)
            {
                result.Add(firstLevelIndex + i);
            }

            return result.ToArray();
        }

        public int GetBuildIndex(int levelNumber)
        {
            return firstLevelIndex + levelNumber - 1;
        }

        /// <summary>
        /// Gets the level number of a given scene by it's build index
        /// E.g. For the first playable level, 1 is returned
        /// Returns -1 if the scene is not a playable level (like the main menu)
        /// </summary>
        /// <param name="scene"></param>
        /// <returns></returns>
        public int GetLevelNumber(Scene scene)
        {
            int buildIndex = scene.buildIndex;

            int levelNumber = buildIndex - firstLevelIndex +1;

            if (levelNumber > levelCount)
            {
                return -1;
            }
            else
            {
                return levelNumber;
            }
        }
    }
}