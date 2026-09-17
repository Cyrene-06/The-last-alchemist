using System;
using System.IO;
using UnityEngine;

namespace LastAlchemist
{
    public static class AlchemySave
    {
        [Serializable] struct SaveHeader { public int version; }
        public static string FilePath => Path.Combine(Application.persistentDataPath, "last-alchemist-v3.json");

        public static AlchemyState Load(out string notice) => LoadFrom(FilePath, out notice);

        public static AlchemyState LoadFrom(string filePath, out string notice)
        {
            notice = "";
            if (!File.Exists(filePath) && !File.Exists(filePath + ".bak")) return new AlchemyState();
            foreach (string path in new[] { filePath, filePath + ".bak" })
            {
                try
                {
                    if (!File.Exists(path)) continue;
                    string json = File.ReadAllText(path);
                    if (JsonUtility.FromJson<SaveHeader>(json).version != AlchemyState.SaveVersion) continue;
                    var state = JsonUtility.FromJson<AlchemyState>(json);
                    if (state == null || !state.IsValid()) continue;
                    if (path.EndsWith(".bak")) notice = "主存档不可用，已从备份恢复。";
                    return state;
                }
                catch (Exception e) when (e is IOException || e is ArgumentException || e is UnauthorizedAccessException)
                { Debug.LogWarning("Alchemy save could not be read: " + e.Message); }
            }
            notice = "存档不可用，本次从新工坊开始；原文件已保留。";
            // Preserve corrupt files for recovery rather than overwriting the only copy.
            foreach (string path in new[] { filePath, filePath + ".bak" })
                try { if (File.Exists(path)) File.Copy(path, path + ".invalid-" + DateTime.UtcNow.Ticks, false); }
                catch (IOException e) { Debug.LogWarning(e.Message); }
                catch (UnauthorizedAccessException e) { Debug.LogWarning(e.Message); }
            return new AlchemyState();
        }

        public static bool Write(AlchemyState state) => WriteTo(state, FilePath);

        public static bool WriteTo(AlchemyState state, string filePath)
        {
            if (!state.IsValid()) { Debug.LogError("Refusing invalid alchemy save."); return false; }
            try
            {
                Directory.CreateDirectory(Path.GetDirectoryName(Path.GetFullPath(filePath)));
                string temp = filePath + ".tmp";
                File.WriteAllText(temp, JsonUtility.ToJson(state, true));
                if (File.Exists(filePath)) File.Replace(temp, filePath, filePath + ".bak");
                else File.Move(temp, filePath);
                return true;
            }
            catch (Exception e) when (e is IOException || e is UnauthorizedAccessException)
            { Debug.LogWarning("Alchemy save failed: " + e.Message); return false; }
        }
    }
}


