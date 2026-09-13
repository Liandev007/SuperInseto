using System;
using System.IO;
using System.Text;
using UnityEngine;

namespace SuperInseto
{
    public enum SaveReadResult { Missing, Loaded, Invalid }

    // Plain storage, independent of scene lifecycle. Never falls back to delete-then-write.
    public sealed class SaveFileStore
    {
        public string FilePath { get; }
        public SaveFileStore(string filePath) { FilePath = filePath; }
        static bool FileError(Exception ex) => ex is IOException || ex is UnauthorizedAccessException
            || ex is ArgumentException || ex is NotSupportedException || ex is System.Security.SecurityException;

        public SaveReadResult Read(out SaveGameData data, out string error)
        {
            data = null; error = null;
            try
            {
                if (!File.Exists(FilePath)) return SaveReadResult.Missing;
                if (new FileInfo(FilePath).Length > 1024 * 1024
                    || !SaveGameData.TryParse(File.ReadAllText(FilePath), out data))
                { error = "Save vazio, inválido ou de versão incompatível."; return SaveReadResult.Invalid; }
                return SaveReadResult.Loaded;
            }
            catch (Exception ex) when (FileError(ex))
            { error = ex.Message; return SaveReadResult.Invalid; }
        }
        public bool Write(SaveGameData data, out string error)
        {
            error = null;
            if (data == null || !data.IsValid()) { error = "Dados de save inválidos; arquivo anterior preservado."; return false; }
            string temporary = FilePath + ".tmp";
            try
            {
                Directory.CreateDirectory(Path.GetDirectoryName(FilePath));
                byte[] bytes = new UTF8Encoding(false).GetBytes(JsonUtility.ToJson(data, true));
                using (var stream = new FileStream(temporary, FileMode.Create, FileAccess.Write, FileShare.None))
                { stream.Write(bytes, 0, bytes.Length); stream.Flush(true); }
                if (File.Exists(FilePath)) File.Replace(temporary, FilePath, null);
                else File.Move(temporary, FilePath);
                return true;
            }
            catch (Exception ex) when (FileError(ex)) { error = ex.Message; return false; }
        }
        public bool Clear(out string error)
        {
            error = null;
            try { File.Delete(FilePath); File.Delete(FilePath + ".tmp"); return true; }
            catch (Exception ex) when (FileError(ex)) { error = ex.Message; return false; }
        }
    }
}
