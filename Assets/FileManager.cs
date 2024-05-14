using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEngine;

public class FileManager : MonoBehaviour
{
    public static bool NewFile(string path)
    {
        Debug.Log("NewFile");
        if (File.Exists(path))
        {
            return true;
        }
        FileStream fileStream = new FileStream(path, 
                                       FileMode.OpenOrCreate, 
                                       FileAccess.ReadWrite, 
                                       FileShare.None);
        fileStream.Close();
        return File.Exists(path);
    }

    public static void WriteLine(string path, string line, bool append = false)
    {
        Debug.Log("WriteLine");
        if (!File.Exists(path))
        {
            throw new Exception($"No such file: '{path}'");
        }
        using (StreamWriter outputFile = new(path, append))
        {
            outputFile.WriteLine(line);
        }
    }
}
