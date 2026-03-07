using System;
using System.IO;
using System.Text;
using System.Text.RegularExpressions;

class Program
{
    static void Main()
    {
        string logicSrc = @"d:\Unity\Server_Game\Assets\GameClient\Logic\Player";
        string logicDest = @"d:\Unity\Server_Game\Assets\GameClient\Logic\Character";

        if (Directory.Exists(logicSrc))
        {
            Directory.Move(logicSrc, logicDest);
        }

        string[] files = Directory.GetFiles(logicDest, "*.*", SearchOption.AllDirectories);
        foreach (string file in files)
        {
            string newFile = file;
            if (Path.GetFileName(file).Contains("Player"))
            {
                string newName = Path.GetFileName(file).Replace("Player", "Character");
                newFile = Path.Combine(Path.GetDirectoryName(file), newName);
                if (file != newFile)
                    File.Move(file, newFile);
            }

            if (newFile.EndsWith(".cs"))
            {
                string content = File.ReadAllText(newFile, Encoding.UTF8);
                content = content.Replace("PlayerEntity", "CharacterEntity");
                content = content.Replace("PlayerConfigSO", "CharacterConfigSO");
                content = content.Replace("PlayerStateBase", "CharacterStateBase");
                content = content.Replace("PlayerGroundState", "CharacterGroundState");
                content = content.Replace("PlayerAirborneState", "CharacterAirborneState");
                content = content.Replace("PlayerCameraController", "CharacterCameraController");
                content = content.Replace("Game.Logic.Player", "Game.Logic.Character");
                content = content.Replace("Game.Config.Player", "Game.Config.Character");
                content = content.Replace("Test_Player", "Test_Character");

                // Save it back using UTF8 encoding without BOM
                File.WriteAllText(newFile, content, new UTF8Encoding(false));
            }
        }
        Console.WriteLine("Renaming and Replacing Done.");
    }
}
