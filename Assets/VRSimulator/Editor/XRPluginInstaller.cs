using System.Collections;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using UnityEditor.PackageManager;
using UnityEditor.PackageManager.Requests;
using UnityEditor.PackageManager.UI;
using System.IO;

[InitializeOnLoad]
public static class XRPluginInstaller
{
    static AddRequest Request;

    internal static string packageName = "com.thrashpanda.vr.simulator";
    static string packageDir = "Assets/VRSimulator";
    static string packageFileName = "com.thrashpanda.vr.simulator*.tgz";
    static string packageTarballPath; 

    static XRPluginInstaller()
    {
        DirectoryInfo dirInfo = new DirectoryInfo(packageDir);
        FileInfo[] fileInfo = dirInfo.GetFiles(packageFileName);
        if(fileInfo.Length > 0)
        {
            packageTarballPath = fileInfo[0].FullName;
        }
        
        EditorApplication.delayCall += () =>
        {
            if (!CheckForPackage())
            {
                InitialNote.ShowWindow();
            }

        };
    }

    public static void InstallPackage()
    {
        if (CheckForPackage())
            Debug.LogWarning("VR Simulator XR Plugin is already installed.");
        else
        {
            Debug.Log("Installing VR Simulator Package ...");
            Request = Client.Add("file:" + Path.GetFullPath(packageTarballPath));
            EditorApplication.update += installProgress;

        }
    }

    public static void InstallSamples()
    {
        if (!CheckForPackage())
            Debug.LogError("VR Simulator XR Plugin is not yet installed. Please install the package before trying to install any samples.");
        else
        {
            IEnumerable<Sample> samples = Sample.FindByPackage(packageName, "");

            foreach (Sample s in samples)
            {
                s.Import();
            }
        }
    }


    static void installProgress()
    {
        if (Request.IsCompleted)
        {
            if (Request.Status == StatusCode.Success)
            {
                Debug.Log("Installed: " + Request.Result.packageId);
            }
            else if (Request.Status >= StatusCode.Failure)
                Debug.Log(Request.Error.message);

            EditorApplication.update -= installProgress;
        }
    }

    public static bool CheckForPackage()
    {
        if (Directory.Exists("Packages/" + packageName))
            return true;
        else
            return false;
    }
}
