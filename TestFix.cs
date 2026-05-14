using System;
using System.IO;
using System.Text;
using System.Text.RegularExpressions;

public class TestFix {
    public static void Run() {
        var gbk = Encoding.GetEncoding(936);
        var strictUtf8 = new UTF8Encoding(false, true);
        string text = File.ReadAllText(@"D:\Unity\ZZZ\Assets\GameClient\ATEditor\Editor\Playback\ATEditorWindow.Preview.cs", Encoding.UTF8);
        
        string newText = Regex.Replace(text, @"[^\x00-\x7F]+", match => {
            try {
                byte[] bytes = gbk.GetBytes(match.Value);
                string decoded = strictUtf8.GetString(bytes);
                if (decoded != match.Value && !decoded.Contains("\uFFFD")) {
                    Console.WriteLine("Replaced: " + match.Value.Substring(0, Math.Min(5, match.Value.Length)) + " -> " + decoded.Substring(0, Math.Min(5, decoded.Length)));
                    return decoded;
                }
            } catch {}
            return match.Value;
        });
    }
}
