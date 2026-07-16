#if UNITY_ANDROID
using System.IO;
using System.Xml;
using UnityEditor.Android;

public sealed class ARUnityXAndroidBuildFix : IPostGenerateGradleAndroidProject
{
    public int callbackOrder => 100;

    public void OnPostGenerateGradleAndroidProject(string path)
    {
        RemoveUnusedNetworkPermissions(path);
        MakeCameraHardwareOptional(path);
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

    private static void MakeCameraHardwareOptional(string path)
    {
        string manifestPath = Path.Combine(path, "src", "main", "AndroidManifest.xml");
        var document = new XmlDocument();
        document.Load(manifestPath);

        var namespaces = new XmlNamespaceManager(document.NameTable);
        namespaces.AddNamespace("android", "http://schemas.android.com/apk/res/android");

        XmlNodeList cameraFeatures = document.SelectNodes(
            "/manifest/uses-feature[@android:name='android.hardware.camera' or " +
            "@android:name='android.hardware.camera.any']",
            namespaces);

        foreach (XmlNode feature in cameraFeatures)
        {
            XmlAttribute required = document.CreateAttribute(
                "android",
                "required",
                "http://schemas.android.com/apk/res/android");
            required.Value = "false";
            feature.Attributes.SetNamedItem(required);
        }

        document.Save(manifestPath);
    }
}
#endif
