#!/usr/bin/env dotnet-script
#r "nuget: YamlDotNet, 11.2.1"
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using YamlDotNet;

Console.WriteLine("pre-commit hook");
Command command = new Command();
StreamReader streamReader = new StreamReader(@"precommit.yaml");
string str = streamReader.ReadToEnd();
streamReader.Close();
YamlDotNet.Serialization.IDeserializer deSerializer = new YamlDotNet.Serialization.DeserializerBuilder().Build();
List<CommandStruct> res = deSerializer.Deserialize<List<CommandStruct>>(str);
command.AddCommandStructs(res);

command.ListExecute();
command.Finish();

public struct CommandStruct
{
    public string symbol;
    public string command;
    public bool check;
}

public class Command
{
    private int checkExitCode;
    private readonly List<CommandStruct> listCommands;
    public Command()
    {
        checkExitCode = 0;
        listCommands = new List<CommandStruct>();
    }

    /// <summary>
    /// 新增指令到List中，這個指令會被執行且確認
    /// </summary>
    /// <param name="symbol">標示</param>
    /// <param name="command">指令內容</param>
    public void AddCommand(string symbol, string command) => listCommands.Add(new CommandStruct() { symbol = symbol, command = command,check = true });

    /// <summary>
    /// 給外部指定使用
    /// </summary>
    /// <param name="commandStructs"></param>
    public void AddCommandStructs(List<CommandStruct> commandStructs) => listCommands.AddRange(commandStructs);

    /// <summary>
    /// 新增指令到List中，這個指令會被執行，但不會進行確認
    /// </summary>
    /// <param name="symbol">標示</param>
    /// <param name="command">指令內容</param>
    public void AddCommandWithoutCheck(string symbol, string command) => listCommands.Add(new CommandStruct() { symbol = symbol, command= command, check = false });

    /// <summary>
    /// 執行所有存在List中的Command，結束時需要呼叫Finish()
    /// </summary>
    public void ListExecute()
    {
        string result = "pre-commit result:";
        foreach(CommandStruct cs in listCommands)
        {
            Process cmd = new Process();
            cmd.StartInfo.FileName = "powershell.exe";
            cmd.StartInfo.RedirectStandardInput = true;
            cmd.StartInfo.RedirectStandardOutput = true;
            cmd.StartInfo.CreateNoWindow = true;
            cmd.StartInfo.UseShellExecute = false;
            cmd.Start();
            string command = cs.command;
            cmd.StandardInput.WriteLine(command);
            cmd.StandardInput.Flush();
            cmd.StandardInput.Close();
            cmd.WaitForExit();
            if (!cs.check)
                continue;
            checkExitCode += cmd.ExitCode;
            if (cmd.ExitCode == 0)
                result += $"\r\n{cs.symbol}: Pass";
            else
            {
                result += $"\r\n{cs.symbol}: Fail";
                Console.WriteLine(cmd.StandardOutput.ReadToEnd());
            }
        }
        Console.WriteLine(result);
    }


    /// <summary>
    /// 這個指令會被執行且確認，結束時需要呼叫Finish()
    /// </summary>
    /// <param name="excuteCommand"></param>
    public void Execute(string excuteCommand)
    {
        Process cmd = new Process();
        cmd.StartInfo.FileName = "powershell.exe";
        cmd.StartInfo.RedirectStandardInput = true;
        cmd.StartInfo.RedirectStandardOutput = false;
        cmd.StartInfo.CreateNoWindow = true;
        cmd.StartInfo.UseShellExecute = false;
        cmd.Start();
        string command = excuteCommand;
        cmd.StandardInput.WriteLine(command);
        cmd.StandardInput.Flush();
        cmd.StandardInput.Close();
        cmd.WaitForExit();
        checkExitCode += cmd.ExitCode;
        if (cmd.ExitCode == 0)
            Console.WriteLine("Pass");
        else
            Console.WriteLine("Fail");
    }

    /// <summary>
    /// 這個指令會被執行，但不會進行確認，結束時需要呼叫Finish()
    /// </summary>
    /// <param name="excuteCommand"></param>
    public void ExecuteWithoutCheck(string excuteCommand)
    {
        Process cmd = new Process();
        cmd.StartInfo.FileName = "cmd.exe";
        cmd.StartInfo.RedirectStandardInput = true;
        cmd.StartInfo.RedirectStandardOutput = false;
        cmd.StartInfo.CreateNoWindow = true;
        cmd.StartInfo.UseShellExecute = false;
        cmd.Start();
        string command = excuteCommand;
        cmd.StandardInput.WriteLine(command);
        cmd.StandardInput.Flush();
        cmd.StandardInput.Close();
        cmd.WaitForExit();
    }

    /// <summary>
    /// 當結束時呼叫
    /// </summary>
    public void Finish()
    {
        int exitCode = checkExitCode == 0 ? 0 : 1;
        Environment.Exit(exitCode);
    }
}