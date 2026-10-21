using System;
using System.IO;
using System.Reflection;
using UnityEditor;

public static class ImportUnityTestSupport
{
    public static string Run()
    {
        const string folder="Assets/_Project/Features/SymbolLoop/Tests/Support";
        if(!AssetDatabase.IsValidFolder(folder)) AssetDatabase.CreateFolder("Assets/_Project/Features/SymbolLoop/Tests","Support");
        string source=Path.GetFullPath("Server/DiceWarrior.Authority/bin/Debug/netstandard2.1/DiceWarrior.Authority.dll");
        string target=folder+"/DiceWarrior.Authority.dll";
        File.Copy(source,target,true);
        AssetDatabase.ImportAsset(target,ImportAssetOptions.ForceSynchronousImport);
        var importer=AssetImporter.GetAtPath(target) as PluginImporter;
        if(importer==null) throw new InvalidOperationException("Test support plugin importer was not created.");
        importer.SetCompatibleWithAnyPlatform(false);
        importer.SetCompatibleWithEditor(true);
        // Unity 6000.6 exposes this importer setting as an internal property, not a public setter.
        var explicitReference=typeof(PluginImporter).GetProperty("IsExplicitlyReferenced",BindingFlags.Instance|BindingFlags.NonPublic);
        if(explicitReference==null) throw new NotSupportedException("This Unity version does not expose the explicit plugin reference setting.");
        explicitReference.SetValue(importer,true);
        importer.SaveAndReimport();
        return "Editor-only test authority imported; explicit test assembly references only.";
    }
}
