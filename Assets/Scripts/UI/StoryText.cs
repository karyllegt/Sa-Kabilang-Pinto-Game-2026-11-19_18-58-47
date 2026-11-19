using System;
using UnityEngine;

namespace SaKabilangPinto.UI
{
    /// <summary>One object in Assets/Story/levels.json.</summary>
    [Serializable]
    public class LevelStory
    {
        public string id;
        public string name;
        public string intro;
        public string clue;
        public string ending;
    }

    /// <summary>
    /// Reads Assets/Story/levels.json (a JSON list of LevelStory objects).
    /// Read only: this code never writes to the file.
    /// </summary>
    public static class StoryText
    {
        [Serializable]
        class Wrapper
        {
            public LevelStory[] levels;
        }

        /// <summary>Returns the story for a level id, or null if it is missing.</summary>
        public static LevelStory Get(TextAsset storyFile, string levelId)
        {
            if (storyFile == null || string.IsNullOrEmpty(levelId)) return null;

            // JsonUtility cannot read a top-level list, so wrap it in an object first.
            string json = storyFile.text.TrimStart('﻿').Trim();
            Wrapper wrapper;
            try
            {
                wrapper = JsonUtility.FromJson<Wrapper>("{\"levels\":" + json + "}");
            }
            catch (Exception e)
            {
                Debug.LogError("Could not read " + storyFile.name + ".json: " + e.Message, storyFile);
                return null;
            }

            if (wrapper == null || wrapper.levels == null) return null;
            foreach (var level in wrapper.levels)
            {
                if (level != null && level.id == levelId) return level;
            }
            return null;
        }

        /// <summary>Returns text, or fallback when text is null or empty.</summary>
        public static string Or(string text, string fallback)
        {
            return string.IsNullOrEmpty(text) ? fallback : text;
        }
    }
}
