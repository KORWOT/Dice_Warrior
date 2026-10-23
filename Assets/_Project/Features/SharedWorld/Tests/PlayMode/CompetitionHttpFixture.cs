#if UNITY_EDITOR
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Security.Cryptography;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using DiceWarrior.SymbolLoop;
using FateDice;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem.UI;
using UnityEngine.UI;
using Object=UnityEngine.Object;

namespace DiceWarrior.SharedWorld.Tests
{
    // This helper owns only its UI/transports and explicitly injected Q1 artifact directory.
    // The coordinator owns the server, database, restart and final artifact cleanup.
    internal sealed class CompetitionHttpFixture : IDisposable
    {
        const string ContentPath="Assets/_Project/Features/SharedWorld/Content/shared-world-preview-4.json";
        const string PrefabPath="Assets/_Project/Features/SharedWorld/Prefabs/CompetitionScreen.prefab";
        public readonly HttpBackendSettings Settings;
        public readonly HttpGameBackend Backend;
        public readonly Q1WireHandler Wire;
        public readonly CompetitionDefinition Definition;
        public readonly string DirectoryPath,AccountPath,CompetitionPath,SelectionPath,LoadoutPath;
        public string RestartEvidencePath=>Path.Combine(DirectoryPath,"restart-evidence.json");
        public CompetitionApp App { get; private set; }
        public CompetitionScreen Screen=>(CompetitionScreen)ui.ActiveScreen;
        readonly HttpClient diagnostics;
        readonly TextAsset content;
        GameObject root,appObject,eventObject,cameraObject;
        UIManager ui;
        Camera camera;
        RenderTexture target;

        public CompetitionHttpFixture(string scenario,bool restart=false,int timeoutSeconds=3)
        {
            string endpoint=Required("DICE_WARRIOR_Q1_URL");
            string injectedRoot=Path.GetFullPath(Required("DICE_WARRIOR_Q1_ROOT"));
            string prefix=Required("DICE_WARRIOR_Q1_USER");
            var address=new Uri(endpoint,UriKind.Absolute);
            Assert.That(address.Scheme,Is.EqualTo("http"),"Q1 uses an explicitly owned HTTP development server.");
            Assert.That(IPAddress.TryParse(address.Host.Trim('[',']'),out var ip) && IPAddress.IsLoopback(ip),Is.True);
            Assert.That(address.Port,Is.Not.EqualTo(5087),"The user's running server must not be used.");
            string allowed=Path.GetFullPath(Path.Combine(Application.dataPath,"..","artifacts","next-phase-06"))+Path.DirectorySeparatorChar;
            Assert.That(injectedRoot.StartsWith(allowed,StringComparison.OrdinalIgnoreCase),Is.True,"Q1 root must be under project artifacts/next-phase-06.");
            Assert.That(Path.GetFileName(injectedRoot),Is.EqualTo("client"));
            Assert.That(Path.GetFileName(Path.GetDirectoryName(injectedRoot)).StartsWith("q1-",StringComparison.Ordinal),Is.True);
            Assert.That(scenario.All(c=>char.IsLetterOrDigit(c) || c=='-'),Is.True);
            string suffix=restart?"restart":Guid.NewGuid().ToString("N");
            Settings=new HttpBackendSettings {endpoint=endpoint,userId=prefix+"_"+(restart?suffix:suffix.Substring(0,12)),timeoutSeconds=timeoutSeconds};
            var uri=Settings.Validate(true);
            Assert.That(new[]{"journey","profile-fault","legacy","restart"},Does.Contain(scenario));
            Assert.That(restart,Is.EqualTo(scenario=="restart"));
            DirectoryPath=Path.Combine(injectedRoot,scenario.Substring(0,1));
            if(!restart) Assert.That(Directory.Exists(DirectoryPath),Is.False,"Use a fresh Q1 root for each full run; earlier evidence is preserved.");
            AccountPath=Settings.CheckpointPath(DirectoryPath);
            CompetitionPath=Path.Combine(Path.GetDirectoryName(AccountPath),"shared-world-preview-v1","checkpoint.json");
            SelectionPath=Path.Combine(Path.GetDirectoryName(CompetitionPath),"adventurer-selection.json");
            LoadoutPath=Path.Combine(Path.GetDirectoryName(CompetitionPath),"lobby-loadout.json");
            string id=new string('a',32);
            var longestPaths=new[]{AccountPath+".backend-error-"+id+".json",AccountPath+".pending-discarded-"+id,
                LoadoutPath+".generation-"+id+".tmp",CompetitionPath+".generation.writing-"+id,
                CompetitionPath+".restarted-"+id,SelectionPath+".writing-"+id};
            Assert.That(longestPaths.Max(p=>p.Length),Is.LessThan(260),"Shorten the owned Q1 root: Unity Windows must create temporary and preserved files too.");
            content=AssetDatabase.LoadAssetAtPath<TextAsset>(ContentPath);
            Assert.That(content,Is.Not.Null);
            Definition=CompetitionContent.Load(content.text);
            Assert.That(Definition.rulesVersion,Is.EqualTo(CompetitionV4Content.Version));
            Wire=new Q1WireHandler();Backend=new HttpGameBackend(Settings,true,Wire);
            diagnostics=new HttpClient(Q1WireHandler.Transport(),true) {BaseAddress=uri,Timeout=TimeSpan.FromSeconds(10)};
            Directory.CreateDirectory(DirectoryPath);
            Write("path-budget.json",new {maximumCharacters=longestPaths.Max(p=>p.Length),limitExclusive=260,
                checkpointCharacters=CompetitionPath.Length,accountCharacters=AccountPath.Length});
            TestContext.WriteLine("Q1 fixture: "+DirectoryPath+"; endpoint="+uri+"; userHash="+LoopJson.Hash(Settings.userId));
        }

        static string Required(string name)
        {
            string value=Environment.GetEnvironmentVariable(name);
            Assert.That(string.IsNullOrWhiteSpace(value),Is.False,"Explicit Q1 configuration missing: "+name);
            return value;
        }

        public IEnumerator Open(bool authored=false)
        {
            var health=Json("/healthz");yield return Await(health);
            Assert.That((string)health.Result["mode"],Is.EqualTo("development-mock"));
            Assert.That((int)health.Result["protocol"],Is.EqualTo(1));
            Assert.That((string)health.Result["rulesVersion"],Is.EqualTo(BackendProtocol.RulesVersion));
            if(!root) BuildUi();
            if(appObject) {Object.Destroy(appObject);yield return null;}
            appObject=new GameObject("Q1 isolated HTTP app");appObject.SetActive(false);
            App=appObject.AddComponent<CompetitionApp>();App.ui=ui;App.content=content;
            App.combatConfig=AssetDatabase.LoadAssetAtPath<SymbolLoopConfig>("Assets/_Project/Features/SymbolLoop/Configs/SymbolLoopConfig.asset");
            Assert.That(App.combatConfig,Is.Not.Null);
            if(authored) App.OpenHttp(Settings,DirectoryPath);
            else App.OpenAccount(AccountPath,CompetitionPath,Definition,Backend,()=>33);
            appObject.SetActive(true);yield return null;yield return WaitIdle();
            Assert.That(ui.ActiveScreen,Is.TypeOf<CompetitionScreen>());
        }

        void BuildUi()
        {
            root=Object.Instantiate(AssetDatabase.LoadAssetAtPath<GameObject>("Assets/_Project/Shared/UI/Prefabs/UIRoot.prefab"));
            ui=root.GetComponentInChildren<UIManager>(true);
            ui.prefabs=new BaseUI[]{AssetDatabase.LoadAssetAtPath<CompetitionScreen>(PrefabPath)};
            cameraObject=new GameObject("Q1 capture camera");camera=cameraObject.AddComponent<Camera>();
            camera.clearFlags=CameraClearFlags.SolidColor;camera.backgroundColor=Color.black;
            target=new RenderTexture(720,1280,24);camera.targetTexture=target;
            ui.Root.canvas.renderMode=RenderMode.ScreenSpaceCamera;ui.Root.canvas.worldCamera=camera;ui.Root.canvas.planeDistance=1;
            ui.Root.GetComponent<CanvasScaler>().uiScaleMode=CanvasScaler.ScaleMode.ConstantPixelSize;ui.Root.canvas.scaleFactor=1;
            eventObject=new GameObject("Q1 pointer events",typeof(EventSystem),typeof(InputSystemUIInputModule));
            Canvas.ForceUpdateCanvases();
        }

        public CommonButtonView Button(string prefix)
        {
            var matches=Screen.buttons.Where(b=>b.gameObject.activeSelf && b.label.text.StartsWith(prefix,StringComparison.Ordinal)).ToArray();
            Assert.That(matches.Length,Is.EqualTo(1),"Expected one visible action: "+prefix+"; visible="+string.Join(" / ",Screen.buttons.Where(b=>b.gameObject.activeSelf).Select(b=>b.label.text)));
            return matches[0];
        }

        void Click(string prefix)
        {
            Canvas.ForceUpdateCanvases();var button=Button(prefix);
            Assert.That(button.button.interactable,Is.True,"Action disabled: "+button.label.text);
            var rect=(RectTransform)button.transform;
            var input=new PointerEventData(eventObject.GetComponent<EventSystem>()) {
                position=RectTransformUtility.WorldToScreenPoint(camera,rect.TransformPoint(rect.rect.center)),button=PointerEventData.InputButton.Left};
            var hits=new List<RaycastResult>();eventObject.GetComponent<EventSystem>().RaycastAll(input,hits);
            var hit=hits.Select(h=>h.gameObject.GetComponentInParent<Button>()).FirstOrDefault(b=>b);
            if(hit!=button.button) Capture("pointer-failure");
            Assert.That(hit,Is.EqualTo(button.button),"Actual pointer did not reach "+button.label.text+" at "+input.position);
            ExecuteEvents.Execute(button.button.gameObject,input,ExecuteEvents.pointerClickHandler);
            Canvas.ForceUpdateCanvases();
        }

        public IEnumerator Press(string prefix)
        {yield return null;Click(prefix);yield return null;yield return WaitIdle();}

        public IEnumerator SelectAdventurer(string id)
        {yield return Press("모험가 선택");yield return Press(Definition.adventurers.Single(a=>a.id==id).name+" 선택");}

        public IEnumerator Step(bool fullPlayback)
        {
            var state=App.Session.Snapshot.state;var p=state.participants[0];string label;
            Assert.That(state.finished,Is.False);
            switch(p.phase)
            {
                case CompetitionPhase.Map:label="이동 주사위 굴리기";break;
                case CompetitionPhase.Combat:label="기호 주사위 굴리기";break;
                case CompetitionPhase.Encounter:
                    var choices=CompetitionCommandRules.LegalChoices(state,p);
                    var heal=choices.FirstOrDefault(c=>c.kind==EncounterKind.Heal);
                    var combat=choices.FirstOrDefault(c=>c.kind==EncounterKind.Combat);
                    var choice=(p.hp*2<p.maxHp?heal:null) ?? combat ?? choices.OrderByDescending(c=>c.xp).ThenBy(c=>c.goldCost).First();
                    label=choice.label;break;
                case CompetitionPhase.LapReward:
                    label=p.completedLaps<state.definition.lapsToFinish-1?state.definition.combat.dice.Single(d=>d.id=="arcane").name+" 주사위 추가":state.definition.combat.gear.Single(g=>g.id=="aegis").name;
                    break;
                case CompetitionPhase.Complete:label="다음 AI 라운드";break;
                default:throw new InvalidOperationException("Unknown human phase");
            }
            yield return null;Click(label);
            Assert.That(App.Session.Snapshot.state.actionNumber,Is.EqualTo(state.actionNumber+1),"A pointer action must commit exactly one local command.");
            if(fullPlayback) {yield return WaitIdle();yield return null;}
            else
            {
                string committed=LoopJson.Encode(App.Session.Snapshot);
                // Only local presentation is interrupted, after the normal command/store path succeeded.
                Assert.That(App.Account.Busy,Is.False);
                appObject.SetActive(false);yield return null;appObject.SetActive(true);yield return null;
                Assert.That(LoopJson.Encode(App.Session.Snapshot),Is.EqualTo(committed));
                Assert.That(App.IsPresenting,Is.False);Canvas.ForceUpdateCanvases();
            }
        }

        IEnumerator WaitIdle()
        {
            float deadline=Time.realtimeSinceStartup+30;
            while(App.IsPresenting) {Assert.That(Time.realtimeSinceStartup,Is.LessThan(deadline),"HTTP/presentation did not finish");yield return null;}
            Canvas.ForceUpdateCanvases();
        }
        public static IEnumerator Await(Task task)
        {
            float deadline=Time.realtimeSinceStartup+30;
            while(!task.IsCompleted) {Assert.That(Time.realtimeSinceStartup,Is.LessThan(deadline),"HTTP task did not finish");yield return null;}
            task.GetAwaiter().GetResult();
        }

        public async Task<JObject> Json(string path)
        {
            using(var response=await diagnostics.GetAsync(path).ConfigureAwait(false))
            {response.EnsureSuccessStatusCode();return JObject.Parse(await response.Content.ReadAsStringAsync().ConfigureAwait(false));}
        }
        public async Task<JObject> Cost()
        {
            using(var body=new StringContent("{}",Encoding.UTF8,"application/json"))
            using(var response=await diagnostics.PostAsync("/dev/costs",body).ConfigureAwait(false))
            {response.EnsureSuccessStatusCode();return JObject.Parse(await response.Content.ReadAsStringAsync().ConfigureAwait(false));}
        }
        public static long Requests(JObject metrics,string kind)
            =>metrics["byEndpoint"].Children().Where(g=>(string)g["key"]==kind).Sum(g=>(long)g["requests"]);
        public static string FileHash(string path)
        {using(var stream=File.OpenRead(path)) using(var hash=SHA256.Create()) return BitConverter.ToString(hash.ComputeHash(stream)).Replace("-","");}
        public void AssertNoGameWrites()
            =>Assert.That(Wire.Records.All(r=>r.method=="GET" && r.path=="/v1/profile"),Is.True,"Local v4 play may only read its server profile.");

        public void Write(string name,object value)
        {
            Assert.That(Path.GetFileName(name),Is.EqualTo(name));
            File.WriteAllText(Path.Combine(DirectoryPath,name),JsonConvert.SerializeObject(value,Formatting.Indented));
        }
        public void Capture(string name)
        {
            Assert.That(Path.GetFileName(name),Is.EqualTo(name));
            Canvas.ForceUpdateCanvases();camera.Render();var previous=RenderTexture.active;var texture=new Texture2D(target.width,target.height,TextureFormat.RGB24,false);
            try {RenderTexture.active=target;texture.ReadPixels(new Rect(0,0,target.width,target.height),0,0);texture.Apply();File.WriteAllBytes(Path.Combine(DirectoryPath,name+".png"),texture.EncodeToPNG());}
            finally {RenderTexture.active=previous;Object.Destroy(texture);}
        }
        public void Dispose()
        {
            if(ui) ui.CloseAll();if(appObject) Object.Destroy(appObject);if(root) Object.Destroy(root);
            if(eventObject) Object.Destroy(eventObject);if(cameraObject) Object.Destroy(cameraObject);
            if(target) {target.Release();Object.Destroy(target);}
            Backend.Dispose();diagnostics.Dispose();
        }
    }

    // Observes real requests and attaches one supported development fault. Never synthesizes a response.
    internal sealed class Q1WireHandler : DelegatingHandler
    {
        readonly object gate=new object();
        readonly List<Q1HttpRecord> records=new List<Q1HttpRecord>();
        string armedMethod,armedPath,armedFault;
        public Q1WireHandler():base(Transport()) {}
        public static HttpClientHandler Transport()=>new HttpClientHandler {AllowAutoRedirect=false,UseCookies=false,UseProxy=false,UseDefaultCredentials=false,Credentials=null};
        public Q1HttpRecord[] Records {get {lock(gate) return records.ToArray();}}
        public int Count(string method,string path)=>Records.Count(r=>r.method==method && r.path==path);
        public void Arm(string method,string path,string fault)
        {
            if(!new[]{"before-abort","after-abort","after-delay"}.Contains(fault)) throw new ArgumentException("Unsupported server fault");
            lock(gate)
            {
                if(armedFault!=null) throw new InvalidOperationException("Previous fault has not been used");
                armedMethod=method;armedPath=path;armedFault=fault;
            }
        }
        protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request,CancellationToken ct)
        {
            string body=request.Content==null?null:await request.Content.ReadAsStringAsync().ConfigureAwait(false);
            var record=new Q1HttpRecord {method=request.Method.Method,path=request.RequestUri.AbsolutePath,
                bodyHash=body==null?null:LoopJson.Hash(body),requestId=body==null?null:(string)JObject.Parse(body)["requestId"]};
            lock(gate)
            {
                if(armedFault!=null && record.method==armedMethod && record.path==armedPath)
                {record.fault=armedFault;request.Headers.Add("X-DiceWarrior-Test-Fault",armedFault);armedFault=null;}
                records.Add(record);
            }
            try {var response=await base.SendAsync(request,ct).ConfigureAwait(false);lock(gate) record.statusCode=(int)response.StatusCode;return response;}
            catch {lock(gate) record.transportError=true;throw;}
        }
        public object Summary()=>Records.GroupBy(r=>r.method+" "+r.path).Select(g=>new {
            route=g.Key,requests=g.Count(),transportErrors=g.Count(r=>r.transportError),faults=g.Where(r=>r.fault!=null).Select(r=>r.fault).ToArray(),
            bodyHashes=g.Where(r=>r.bodyHash!=null).Select(r=>r.bodyHash).Distinct().ToArray()}).ToArray();
    }
    internal sealed class Q1HttpRecord
    {
        public string method,path,requestId,bodyHash,fault;
        public bool transportError;
        public int statusCode;
    }
}
#endif
