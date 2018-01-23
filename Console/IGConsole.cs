﻿using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace PiTung_Bootstrap.Console
{
    /// <summary>
    /// Type of a log (should be self-explanatory)
    /// </summary>
    public enum LogType
    {
        INFO,
        USERINPUT,
        ERROR
    }

    /// <summary>
    /// A line of log. It has a message and a log type
    /// </summary>
    internal class LogEntry
    {
        public LogType Type { get; private set; }
        public string Message { get; private set; }

        public LogEntry(LogType type, string message)
        {
            this.Type = type;
            this.Message = message;
        }

        public Color GetColor()
        {
            switch(Type)
            {
                case LogType.INFO:
                    return Color.white;
                case LogType.USERINPUT:
                    return Color.cyan;
                case LogType.ERROR:
                    return Color.red;
            }
            return Color.white;
        }
    }

    /// <summary>
    /// Represents a command that can be invoked from the console
    /// </summary>
    public abstract class Command
    {
        /// <summary>
        /// Used to invoke the command (e.g. "help")
        /// </summary>
        public abstract string Name { get; }

        /// <summary>
        /// How to use the command (e.g. $"{Name} argument [optional_argument]")
        /// </summary>
        public abstract string Usage { get; }

        /// <summary>
        /// Short description of what the command does, preferably on 1 line
        /// </summary>
        public virtual string Description { get; } = null;

        /// <summary>
        /// Called when the command is invoked
        /// </summary>
        /// <param name="arguments">The arguments given to the command</param>
        public abstract bool Execute(IEnumerable<string> arguments);
    }

    //FIXME: Changing scene to gameplay locks mouse
    //TODO: Add verbosity levels

    /// <summary>
    /// In game console
    /// <para>This static class allows for logging and registering commands
    /// which will be executed by callbacks. Contains a dictionary of
    /// global variables that can be read and written to from the console
    /// using the "set" command</para>
    /// </summary>
    public static class IGConsole
    {
        /// <summary>
        /// Max number of log entries kept in memory
        /// </summary>
        private const int MaxHistory = 100;

        /// <summary>
        /// Height of lines in pixels
        /// </summary>
        private const int LineHeight = 16;

        /// <summary>
        /// Input prompt, displayed in front of the user input
        /// </summary>
        private const string Prompt = "> ";

        /// <summary>
        /// Cursor displayed at edit location
        /// </summary>
        private const string Cursor = "_";


        /// <summary>
        /// Console text style (font mostly)
        /// </summary>
        private static GUIStyle Style;

        /// <summary>
        /// Log of user input and command output
        /// </summary>
        private static DropOutStack<LogEntry> CmdLog;

        /// <summary>
        /// Command history for retrieval with up and down arrows
        /// </summary>
        private static DropOutStack<String> History;

        /// <summary>
        /// Where the cursor is within the line
        /// </summary>
        private static int EditLocation = 0;

        /// <summary>
        /// Where we are in the command history
        /// </summary>
        private static int HistorySelector = -1;

        /// <summary>
        /// What is currently in the input line
        /// </summary>
        private static string CurrentCmd = "";

        /// <summary>
        /// Command registry (name -> Command)
        /// </summary>
        internal static Dictionary<string, Command> Registry;

        /// <summary>
        /// Variable registry (name -> value)
        /// </summary>
        private static Dictionary<string, string> VarRegistry;

        /// <summary>
        /// Time at which the show-hide animation started.
        /// </summary>
        private static float ShownAtTime = 0;

        /// <summary>
        /// Is the console currently shown?
        /// </summary>
        internal static bool Shown = false;

        /// <summary>
        /// Show-hide animation duration.
        /// </summary>
        internal static float ShowAnimationTime = .3f;



        /// <summary>
        /// Call this function before doing anything with the console
        /// </summary>
        internal static void Init()
        {
            CmdLog = new DropOutStack<LogEntry>(MaxHistory);
            History = new DropOutStack<string>(MaxHistory);
            Registry = new Dictionary<string, Command>();
            VarRegistry = new Dictionary<string, string>();
            Style = new GUIStyle
            {
                font = Font.CreateDynamicFontFromOSFont("Consolas", 16),
                richText = true
            };

            RegisterCommand<Command_help>();
            RegisterCommand<Command_lsmod>();
            RegisterCommand<Command_set>();
            RegisterCommand<Command_get>();

            Log("Console initialized");
            Log("Type \"help\" to get a list of commands");
        }

        /// <summary>
        /// Call this function on Update calls
        /// </summary>
        internal static void Update()
        {
            // Toggle console with TAB
            if (Input.GetKeyDown(KeyCode.Tab))
            {
                Shown = !Shown;

                float off = 0;

                //The user is toggling the console but the animation hasn't yet ended, resume it later
                if (Time.time - ShownAtTime < ShowAnimationTime)
                {
                    off -= ShowAnimationTime - (Time.time - ShownAtTime);
                }

                ShownAtTime = Time.time + off;

                if(SceneManager.GetActiveScene().name == "gameplay")
                {
                    if (Shown)
                        UIManager.UnlockMouseAndDisableFirstPersonLooking();
                    else if(!UIManager.SomeOtherMenuIsOpen)
                        UIManager.LockMouseAndEnableFirstPersonLooking();
                }
            }

            if (Shown)
            {
                // Handling history
                if (Input.GetKeyDown(KeyCode.UpArrow) && HistorySelector < History.Count - 1)
                {
                    HistorySelector += 1;
                    CurrentCmd = History.Get(HistorySelector);
                    EditLocation = CurrentCmd.Length;
                }
                if (Input.GetKeyDown(KeyCode.DownArrow) && HistorySelector > -1)
                {
                    HistorySelector -= 1;
                    if (HistorySelector == -1)
                        CurrentCmd = "";
                    else
                        CurrentCmd = History.Get(HistorySelector);
                    EditLocation = CurrentCmd.Length;
                }
                // Handle editing
                if (Input.GetKeyDown(KeyCode.LeftArrow) && EditLocation > 0)
                    EditLocation--;
                if (Input.GetKeyDown(KeyCode.RightArrow) && EditLocation < CurrentCmd.Length)
                    EditLocation++;

                ReadInput(); // Read text input
            }
        }

        /// <summary>
        /// Call this function on OnGUI calls
        /// </summary>
        internal static void Draw()
        {
            if (!Shown && Time.time - ShownAtTime > ShowAnimationTime)
            {
                return;
            }

            Color background = Color.black;
            background.a = 0.75f;

            int height = Screen.height / 2;
            int width = Screen.width;
            int linecount = height / LineHeight;

            float yOffset = 0;

            if (Time.time - ShownAtTime < ShowAnimationTime)
            {
                int a = Shown ? height : 0;
                int b = Shown ? 0 : height;

                yOffset = -EaseOutQuad(a, b, (Time.time - ShownAtTime) / ShowAnimationTime);
            }

            // Background rectangle
            ModUtilities.Graphics.DrawRect(new Rect(0, yOffset, width, height + 5), background);

            for(int line = 0; line < Math.Min(linecount - 1, CmdLog.Count); line++)
            {
                LogEntry entry = CmdLog.Get(line);
                int y = (linecount - 2 - line) * LineHeight;
                DrawText(entry.Message, new Vector2(5, y + yOffset), entry.GetColor());
            }

            float consoleY = (linecount - 1) * LineHeight + yOffset;

            try
            {
                DrawText(Prompt + CurrentCmd, new Vector2(5, consoleY), Color.green);
                float x = Width(Prompt) + Width(CurrentCmd.Substring(0, EditLocation));
                DrawText(Cursor, new Vector2(5 + x, consoleY), Color.green);
            }
            catch (Exception e)
            {
                Error($"currentCmd: \"{CurrentCmd}\"\neditLocation: {EditLocation}");
                Error(e.ToString());
                CurrentCmd = "";
                EditLocation = 0;
            }

            float Width(string text) => Style.CalcSize(new GUIContent(text)).x;
        }

        /// <summary>
        /// Log a message to the console (can be multi-line)
        /// </summary>
        /// <param name="type">Type of log <see cref="LogType"/></param>
        /// <param name="msg">Message to log</param>
        public static void Log(LogType type, string msg)
        {
            string[] lines = msg.Split('\n');
            foreach(string line in lines)
            {
                CmdLog.Push(new LogEntry(type, line));
            }
        }

        /// <summary>
        /// Logs a message as simple info
        /// </summary>
        /// <param name="msg">Message to log</param>
        public static void Log(string msg)
        {
            Log(LogType.INFO, msg);
        }

        /// <summary>
        /// Saves the value of a global variable
        /// </summary>
        /// <param name="variable">The variable to set</param>
        /// <param name="value">The value to give</param>
        internal static void SetVariable(string variable, string value)
        {
            VarRegistry[variable] = value;
        }

        /// <summary>
        /// Obtains the value of a global variable
        /// </summary>
        /// <param name="variable">The variable to get</param>
        /// <returns>The value, or null if variable is not set</returns>
        public static string GetVariable(string variable)
        {
            string value;
            if(VarRegistry.TryGetValue(variable, out value))
                return value;
            return null;
        }

        /// <summary>
        /// Logs an error message
        /// </summary>
        /// <param name="msg">Message to log</param>
        public static void Error(string msg)
        {
            Log(LogType.ERROR, msg);
        }

        /// <summary>
        /// Register a command.
        /// </summary>
        /// <param name="command">The command class.</param>
        public static bool RegisterCommand(Command command)
        {
            if (Registry.ContainsKey(command.Name))
                return false;
            Registry.Add(command.Name, command);
            return true;
        }

        /// <summary>
        /// Register a command.
        /// </summary>
        /// <typeparam name="T">The type of the command.</typeparam>
        /// <returns></returns>
        public static bool RegisterCommand<T>() where T : Command
        {
            return RegisterCommand(Activator.CreateInstance<T>());
        }

        /// <summary>
        /// Removes a command from the registry
        /// </summary>
        /// <param name="name">Name of the command to remove</param>
        /// <returns>True if a command was removed, false otherwise</returns>
        public static bool UnregisterCommand(string name)
        {
            if(Registry.ContainsKey(name))
            {
                Registry.Remove(name);
                return true;
            }
            return false;
        }

        /// <summary>
        /// Called when the user presses enter
        /// </summary>
        /// <param name="cmd">The full command line</param>
        private static void ExecuteCommand(string cmd)
        {
            if (cmd.Length == 0)
                return;

            string verb, error;
            string[] args;

            if (!CmdParser.TryParseCmdLine(cmd, out verb, out args, out error))
            {
                Log(LogType.ERROR, "Invalid command: " + error);
                return;
            }
            
            Command command;

            if(Registry.TryGetValue(verb, out command))
            {
                try
                {
                    command.Execute(args);
                }
                catch (Exception e)
                {
                    Log(LogType.ERROR, e.ToString());
                }
            }
            else
            {
                Log(LogType.ERROR, $"Unrecognized command: {verb}");
            }
        }

        private static void ReadInput()
        {
            foreach(char c in Input.inputString)
            {
                if (c == '\b') // has backspace/delete been pressed?
                {
                    if (CurrentCmd.Length != 0)
                    {
                        string firstHalf = CurrentCmd.Substring(0, EditLocation - 1);
                        string secondHalf = CurrentCmd.Substring(EditLocation, CurrentCmd.Length - EditLocation);
                        CurrentCmd = firstHalf + secondHalf;
                        EditLocation--;
                    }
                }
                else if (c == 0x7F) // Ctrl + Backspace (erase word)
                {
                    if (CurrentCmd.Length != 0)
                    {
                        int index = EditLocation;
                        while(index > 0 && Char.IsLetterOrDigit(CurrentCmd.ElementAt(index - 1)))
                            index--;
                        if (index == EditLocation && EditLocation > 0) // Delete at least 1 character
                            index--;
                        int length = EditLocation - index;
                        string firstHalf = CurrentCmd.Substring(0, index);
                        string secondHalf = CurrentCmd.Substring(EditLocation, CurrentCmd.Length - EditLocation);
                        CurrentCmd = firstHalf + secondHalf;
                        EditLocation -= length;
                    }
                }
                else if ((c == '\n') || (c == '\r')) // enter/return
                {
                    if (!string.IsNullOrEmpty(CurrentCmd.Trim()))
                    {
                        Log(LogType.USERINPUT, "> " + CurrentCmd);
                        History.Push(CurrentCmd);
                        ExecuteCommand(CurrentCmd);
                    }

                    CurrentCmd = "";
                    EditLocation = 0;
                }
                else
                {
                    CurrentCmd = CurrentCmd.Insert(EditLocation, c.ToString());
                    EditLocation++;
                }
            }
        }

        private static float EaseOutQuad(float start, float end, float value)
        {
            end -= start;
            return -end * value * (value - 2) + start;
        }
        
        private static void DrawText(string text, Vector2 pos, Color color)
        {
            GUIStyle newStyle = new GUIStyle(Style);
            newStyle.normal.textColor = color;
            Vector2 size = Style.CalcSize(new GUIContent(text));
            Rect rect = new Rect(pos, size);

            GUI.Label(rect, text, newStyle);
        }
    }
}
