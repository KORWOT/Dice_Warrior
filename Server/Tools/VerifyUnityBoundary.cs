using System;
using System.IO;
using System.Linq;
using System.Reflection;
using DiceWarrior.SymbolLoop;
using UnityEditor;
using UnityEditor.Compilation;

public static class VerifyUnityBoundary
{
    public static object Run()
    {
        const string dll="Assets/_Project/Features/SymbolLoop/Tests/Support/DiceWarrior.Authority.dll";
        var importer=(PluginImporter)AssetImporter.GetAtPath(dll);
        bool explicitOnly=(bool)typeof(PluginImporter).GetProperty("IsExplicitlyReferenced",BindingFlags.Instance|BindingFlags.NonPublic).GetValue(importer);
        bool playerReference=CompilationPipeline.GetAssemblies(AssembliesType.Player).Any(a=>
            a.compiledAssemblyReferences.Any(r=>Path.GetFileName(r)=="DiceWarrior.Authority.dll"));
        if(importer.GetCompatibleWithAnyPlatform() || !importer.GetCompatibleWithEditor() ||
            importer.GetCompatibleWithPlatform(BuildTarget.Android) || !explicitOnly || playerReference)
            throw new InvalidOperationException("Authority test DLL leaked across the Editor/player boundary.");
        var assets=AssetDatabase.FindAssets("t:SymbolLoopConfig");
        if(assets.Length!=1) throw new InvalidOperationException("Expected one authoritative Unity MVP config.");
        var config=AssetDatabase.LoadAssetAtPath<SymbolLoopConfig>(AssetDatabase.GUIDToAssetPath(assets[0]));
        var serverRules=LoopJson.Decode<Rules>(File.ReadAllText("Server/DiceWarrior.MockServer/Content/symbol-loop-mvp-1.json"));
        var hash=LoopJson.Hash(LoopJson.Encode(serverRules));
        if(LoopJson.Encode(config.CreateRules())!=LoopJson.Encode(serverRules)) throw new InvalidOperationException("Unity and server content diverged.");
        Environment.SetEnvironmentVariable("DICE_WARRIOR_HTTP_TEST_URL",File.ReadAllText("Temp/standalone-mock-server/unity-test-url.txt").Trim());
        return new {editorOnly=true,explicitReferences=true,playerAuthorityReference=false,rulesMatch=true,rulesSha256=hash,
            sharedJsonAssembly=typeof(LoopJson).Assembly.GetName().Name};
    }
    public static string ClearTestContext()
    {Environment.SetEnvironmentVariable("DICE_WARRIOR_HTTP_TEST_URL",null);return "HTTP test endpoint cleared.";}
}
