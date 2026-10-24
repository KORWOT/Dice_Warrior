using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using UnityEditor.TestTools.TestRunner.Api;
using UnityEngine;

// Editor run_script diagnostic only; never linked into a game or authority service.
public static class V5DiceCombatValidation
{
    public static async Task<object> InspectDiscovery()
    {
        var api=ScriptableObject.CreateInstance<TestRunnerApi>();
        var done=new TaskCompletionSource<object>();
        try
        {
            api.RetrieveTestList(TestMode.PlayMode,root=>
            {
                try
                {
                    var rows=new List<object>();
                    void Visit(ITestAdaptor node)
                    {
                        if(node.IsSuite)rows.Add(new {name=node.FullName,state=node.RunState.ToString(),count=node.TestCaseCount});
                        if(node.Children!=null)foreach(var child in node.Children)Visit(child);
                    }
                    Visit(root);
                    var type=AppDomain.CurrentDomain.GetAssemblies().Select(a=>a.GetType("Unity.Pipeline.Editor.Testing.PipelineTestRunner")).First(t=>t!=null);
                    var collect=type.GetMethod("CollectTestNames",BindingFlags.NonPublic|BindingFlags.Static);
                    var normal=(List<string>)collect.Invoke(null,new object[]{root,"CompetitionFlowTests","testname",false});
                    var explicitAllowed=(List<string>)collect.Invoke(null,new object[]{root,"CompetitionFlowTests","testname",true});
                    done.SetResult(new {suites=rows,selected=normal,selectedWithExplicit=explicitAllowed});
                }
                catch(Exception error){done.SetException(error);}
            });
            return await done.Task;
        }
        finally{UnityEngine.Object.DestroyImmediate(api);}
    }
}
