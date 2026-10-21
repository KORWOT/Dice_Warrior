using System.Diagnostics;
using System.Net;
using System.Net.Sockets;

namespace DiceWarrior.HttpTests;

internal sealed class ServerProcess : IDisposable
{
    readonly Process process;
    bool disposed;
    public string Url {get;}
    public string Database {get;}
    ServerProcess(Process process,string url,string database) {this.process=process;Url=url;Database=database;}
    public static async Task<ServerProcess> Start(string database,uint seed=33,bool faults=true)
    {
        var root=new DirectoryInfo(AppContext.BaseDirectory);
        while(root!=null&&!Directory.Exists(Path.Combine(root.FullName,"Assets"))) root=root.Parent;
        if(root==null) throw new InvalidOperationException("Repository root was not found.");
        var dll=Path.Combine(root.FullName,"Server","DiceWarrior.MockServer","bin","Debug","net10.0","DiceWarrior.MockServer.dll");
        var listener=new TcpListener(IPAddress.Loopback,0); listener.Start();
        int port=((IPEndPoint)listener.LocalEndpoint).Port; listener.Stop();
        string url=$"http://127.0.0.1:{port}";
        var start=new ProcessStartInfo("dotnet") {UseShellExecute=false,CreateNoWindow=true,RedirectStandardOutput=true,RedirectStandardError=true};
        foreach(var arg in new[]{dll,"--environment","Development","--urls",url,"--Mock:DatabasePath",database,
            "--Mock:Seed",seed.ToString(System.Globalization.CultureInfo.InvariantCulture),"--Mock:EnableFaults",faults.ToString()}) start.ArgumentList.Add(arg);
        var process=Process.Start(start)??throw new InvalidOperationException("Could not start Mock process.");
        var output=new System.Text.StringBuilder();
        process.OutputDataReceived+=(_,e)=>{lock(output) {if(output.Length<4096) output.AppendLine(e.Data);}};
        process.ErrorDataReceived+=(_,e)=>{lock(output) {if(output.Length<4096) output.AppendLine(e.Data);}};
        process.BeginOutputReadLine(); process.BeginErrorReadLine();
        var server=new ServerProcess(process,url,database);
        try
        {
            using var http=new HttpClient {Timeout=TimeSpan.FromSeconds(1)};
            for(int i=0;i<100;i++)
            {
                if(process.HasExited) throw new InvalidOperationException("Mock exited: "+output);
                try {if((await http.GetAsync(url+"/healthz")).IsSuccessStatusCode) return server;} catch(HttpRequestException){} catch(TaskCanceledException){}
                await Task.Delay(100);
            }
            throw new TimeoutException("Mock health check did not become ready: "+output);
        }
        catch {server.Dispose();throw;}
    }
    public void Dispose()
    {
        if(disposed) return;
        disposed=true;
        // This object owns only the process it launched. Abrupt exit also exercises SQLite WAL recovery.
        if(!process.HasExited) {process.Kill(entireProcessTree:true);process.WaitForExit(10000);}
        process.Dispose();
    }
}
