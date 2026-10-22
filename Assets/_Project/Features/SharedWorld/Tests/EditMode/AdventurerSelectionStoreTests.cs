using System;
using System.IO;
using DiceWarrior.SymbolLoop;
using NUnit.Framework;

namespace DiceWarrior.SharedWorld.Tests
{
    public sealed class AdventurerSelectionStoreTests
    {
        string directory,path;
        static readonly string[] Ids={"wanderer","scholar","vanguard"};
        [SetUp] public void Setup()
        {
            directory=Path.Combine(Path.GetTempPath(),"DiceWarriorSelectionTests",Guid.NewGuid().ToString("N"));
            path=Path.Combine(directory,"adventurer-selection.json");
        }
        [TearDown] public void Cleanup()
        {
            string allowed=Path.GetFullPath(Path.Combine(Path.GetTempPath(),"DiceWarriorSelectionTests"))+Path.DirectorySeparatorChar;
            if(Path.GetFullPath(directory).StartsWith(allowed,StringComparison.OrdinalIgnoreCase) && Directory.Exists(directory)) Directory.Delete(directory,true);
        }
        [Test] public void SelectionPersistsSeparatelyAndStaleWriterCannotReplaceIt()
        {
            var first=new AdventurerSelectionStore(path,Ids);var second=new AdventurerSelectionStore(path,Ids);
            Assert.That(first.Read().adventurerId,Is.EqualTo("wanderer"));Assert.That(File.Exists(path),Is.False);
            var stale=second.Read();var chosen=first.Select("scholar",0);
            Assert.That(chosen.revision,Is.EqualTo(1));Assert.That(second.Read().adventurerId,Is.EqualTo("scholar"));
            string original=File.ReadAllText(path);
            Assert.Throws<InvalidOperationException>(()=>second.Select("vanguard",stale.revision));
            Assert.Throws<ArgumentException>(()=>first.Select("locked-or-unknown",chosen.revision));
            Assert.That(File.ReadAllText(path),Is.EqualTo(original));
            first.Select("vanguard",1);Assert.That(File.ReadAllText(path+".backup"),Is.EqualTo(original));
            Assert.That(new AdventurerSelectionStore(path,Ids).Read().adventurerId,Is.EqualTo("vanguard"));
        }
        [Test] public void CorruptionMissingPrimaryAndWriteFailurePreserveOriginalBytes()
        {
            var store=new AdventurerSelectionStore(path,Ids);store.Select("scholar",0);string original=File.ReadAllText(path);
            using(var held=new FileStream(path,FileMode.Open,FileAccess.ReadWrite,FileShare.None))
                Assert.Throws<IOException>(()=>store.Select("vanguard",1));
            Assert.That(File.ReadAllText(path),Is.EqualTo(original));
            File.WriteAllText(path,"corrupt selection");
            Assert.Throws<IOException>(()=>store.Read());Assert.Throws<IOException>(()=>store.Select("wanderer",0));
            Assert.That(File.ReadAllText(path),Is.EqualTo("corrupt selection"));
            File.WriteAllText(path,original);store.Select("vanguard",1);File.Delete(path);
            Assert.Throws<IOException>(()=>store.Read());Assert.That(File.Exists(path+".backup"),Is.True);
        }
        [Test] public void InvalidOrOversizedSelectionIsNotSilentlyReset()
        {
            Directory.CreateDirectory(directory);File.WriteAllText(path,new string('x',17000));
            Assert.Throws<IOException>(()=>new AdventurerSelectionStore(path,Ids).Read());
            Assert.That(new FileInfo(path).Length,Is.EqualTo(17000));
        }
    }
}
