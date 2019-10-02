using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Media;
using System.Reflection;
using System.Windows.Forms;
using Checkers;
using Microsoft.Win32;

namespace uclip
{
    public class Program
    {
        public const string PROTOCOL_NAME = "uclip";
        public const string REG_DEFAULT_VALUE = PROTOCOL_NAME + ":copy to clipboard";

        [Command(Description = "Print formats of data in clipboard")]
        public void List()
        {
            var dataObject = Clipboard.GetDataObject();
            if (dataObject != null)
            {
                foreach (var format in dataObject.GetFormats())
                    Console.WriteLine(format);
            }
        }

        [Command(Description = "Add data to clipboard")]
        public void Set(string text, string format = "Text")
        {
            Clipboard.SetData(format, text);
        }

        [Command(Description = "Clear all or some data in clipboard")]
        public void Clear(
            [Option("--only|-o", Description = "Comma separated list of formats, delete only specified formats")]
            string only = null,
            [Option("--except|-e", Description =
                "Comma separated list of formats, delete all formats except specified")]
            string except = null)
        {
            if (only == null && except == null)
            {
                Clipboard.Clear();
                return;
            }

            var onlyFormats = only?.Split(',');
            var exceptFormats = except?.Split(',');

            IDataObject obj = Clipboard.GetDataObject();
            if (obj == null) return;

            DataObject newObj = new DataObject();

            var formats = obj.GetFormats();
            foreach (var format in formats)
            {
                if (onlyFormats != null && !onlyFormats.Contains(format)) continue; // leave all, clear only onlyFormat
                if (exceptFormats != null && exceptFormats.Contains(format))
                    continue; // leave only exceptFormats, clear others
                var data = obj.GetData(format);
                newObj.SetData(format, data);
            }

            Clipboard.SetDataObject(newObj, true);
        }

        [Command(Description = "Add data to clipboard from url text")]
        public void SetFromUri(string text, string format = "Text")
        {
//            text is like: %22say%20hi%20again%22
            text = StringHelper.TrimStart(PROTOCOL_NAME + ":", text);
            text = Uri.UnescapeDataString(text);
            Clipboard.SetData(format, text);
            using (var soundPlayer = new SoundPlayer("Resources/added.wav"))
                soundPlayer.PlaySync();
        }

        [Command(Description = "Register uclip handler in registry, should be executed with admin rights")]
        public void Register()
        {
            var exePath = Assembly.GetEntryAssembly().Location;
            ;
            Registry.ClassesRoot.CreateSubKey(PROTOCOL_NAME);
            using (var key = Registry.ClassesRoot.OpenSubKey(PROTOCOL_NAME, true))
            {
                Check.NotNull(key, $"Can not open registry subkey {PROTOCOL_NAME} for write");
                key.SetValue("", REG_DEFAULT_VALUE);
                key.SetValue("URL Protocol", "", RegistryValueKind.String);
                var cmd = key.CreateSubKey("shell")?.CreateSubKey("open")?.CreateSubKey("command");
                if (cmd != null)
                {
                    cmd.SetValue("", $"{exePath} setFromUri \"%1\"");
                    Console.WriteLine($"{exePath} was registered as {PROTOCOL_NAME} handler");
                }
                else
                {
                    throw new Exception("Can not create command handler in registry");
                }
            }
        }

        [Command(Description = "Unregister uclip handler in registry, should be executed with admin rights")]
        public void UnRegister()
        {
        }

        [Command(IsDefault = true, Description =
            "Print clipboard content in specified format. If no format specified" +
            " print text")]
        public void Get(string format = null)
        {
            var res = format == null ? Clipboard.GetText() : Clipboard.GetData(format);
            Console.WriteLine(res);
        }

        [STAThread]
        public static void Main(string[] args)
        {
            try
            {
                /*CommandLine cmd = new CommandLine();
                cmd.PrintHelp();
                return;*/

                var asm = Assembly.GetEntryAssembly();
                if (asm != null)
                {
                    var exeDir = Path.GetDirectoryName(asm.Location);
                    Directory.SetCurrentDirectory(exeDir); // to find sound files
                }

                CommandLine.Execute(new Program(), args);
            }
            catch (Exception e)
            {
                var innerEx = GetDeepestException(e);
                if (innerEx is UnauthorizedAccessException)
                {
                    Console.WriteLine("You don't have enough permissions, try to run as admin: " + innerEx.Message);
                }
                else Console.WriteLine("Exception in program: " + e);
            }
        }

        private static Exception GetDeepestException(Exception e)
        {
            while (e.InnerException != null) e = e.InnerException;
            return e;
        }
    }
}