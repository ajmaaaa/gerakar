#if UNITY_ANDROID
using System.IO;
using System.Xml;
using UnityEditor.Android;
using UnityEngine;

public sealed class ARUnityXAndroidBuildFix : IPostGenerateGradleAndroidProject
{
    private static readonly string[] SupportedAbis =
    {
        "armeabi-v7a",
        "arm64-v8a"
    };

    public int callbackOrder => 100;

    public void OnPostGenerateGradleAndroidProject(string path)
    {
        foreach (string abi in SupportedAbis)
        {
            RemoveBundledCppRuntime(path, abi);
        }

        RemoveUnusedNetworkPermissions(path);
    }

    private static void RemoveBundledCppRuntime(string path, string abi)
    {
        string bundledRuntime = Path.Combine(
            path,
            "src",
            "main",
            "jniLibs",
            abi,
            "libc++_shared.so");

        if (!File.Exists(bundledRuntime))
        {
            return;
        }

        // Unity IL2CPP supplies the matching libc++ runtime for every enabled ABI.
        // Keeping ARUnityX's second copy makes AGP 9 reject duplicate native files.
        File.Delete(bundledRuntime);
        Debug.Log($"[GerakAR] Removed duplicate ARUnityX C++ runtime for {abi}.");
    }

    private static void RemoveUnusedNetworkPermissions(string path)
    {
        string manifestPath = Path.Combine(path, "src", "main", "AndroidManifest.xml");
        var document = new XmlDocument();
        document.Load(manifestPath);

        var namespaces = new XmlNamespaceManager(document.NameTable);
        namespaces.AddNamespace("android", "http://schemas.android.com/apk/res/android");

        RemovePermission(document, namespaces, "android.permission.INTERNET");
        RemovePermission(document, namespaces, "android.permission.ACCESS_NETWORK_STATE");
        document.Save(manifestPath);
    }

    private static void RemovePermission(
        XmlDocument document,
        XmlNamespaceManager namespaces,
        string permission)
    {
        XmlNode node = document.SelectSingleNode(
            $"/manifest/uses-permission[@android:name='{permission}']",
            namespaces);
        node?.ParentNode?.RemoveChild(node);
    }
}
#endif
