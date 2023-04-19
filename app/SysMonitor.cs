using System;
using System.Diagnostics;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Text.RegularExpressions;
using System.Management;


public static class SysMonitor
{
    public class NvidiaInfo
    {
        public int Utilization { get; set; }
        public int MemoryUsed { get; set; }
        public int MemoryTotal { get; set; }
    }
    public static string GetRAMUsage()
    {
        PerformanceCounter ramCounter = new PerformanceCounter("Memory", "Available Bytes");
        float availableBytes = ramCounter.NextValue();
        float totalBytes = new Microsoft.VisualBasic.Devices.ComputerInfo().TotalPhysicalMemory;
        float usedBytes = totalBytes - availableBytes;

        float usedGB = usedBytes / (1024 * 1024 * 1024);
        float totalGB = totalBytes / (1024 * 1024 * 1024);

        double availableMegabytes = availableBytes / (1024 * 1024);
        Console.WriteLine("Available RAM: {0} MB", availableMegabytes);
        Debug.Print("Available RAM: {0} MB", availableMegabytes);
        return $"{usedGB:F2} GB / {totalGB:F2} GB";
    }

    public static string GetCPUUsage()
    {
        var cpuUsage = new PerformanceCounter("Processor", "% Processor Time", "_Total");
        cpuUsage.NextValue();
        System.Threading.Thread.Sleep(1000);
        double usage = Math.Round(cpuUsage.NextValue(), 2);

        return $"{usage}%";
    }

    public static string GetCPUCoreUsage()
    {
        int coreCount = Environment.ProcessorCount;
        var cpuUsagePerCore = new PerformanceCounter[coreCount];
        for (int i = 0; i < coreCount; i++)
        {
            cpuUsagePerCore[i] = new PerformanceCounter("Processor", "% Processor Time", i.ToString());
            cpuUsagePerCore[i].NextValue();
        }
        System.Threading.Thread.Sleep(1000);
        string coreUsage = "";
        for (int i = 0; i < coreCount; i++)
        {
            double usage = Math.Round(cpuUsagePerCore[i].NextValue(), 2);
            coreUsage += $"C{i + 1}: {usage}%, ";
        }

        return $"{coreUsage.TrimEnd(new char[] { ',', ' ' })}";
    }

    public static NvidiaInfo GetUtilization()
    {
        var psi = new ProcessStartInfo("nvidia-smi", "-q -d UTILIZATION,MEMORY")
        {
            RedirectStandardOutput = true,
            UseShellExecute = false,
            CreateNoWindow = true
        };

        var process = Process.Start(psi);
        var output = process.StandardOutput.ReadToEnd();
        process.WaitForExit();

        var matchUtilization = Regex.Match(output, @"GPU\s+Utilization.*\n.*\n.*\n\s+(\d+)%\s+.*");
        var matchMemoryUsed = Regex.Match(output, @"FB Memory Usage.*\n.*\n\s+(\d+) MiB / (\d+) MiB.*");

        return new NvidiaInfo
        {
            Utilization = matchUtilization.Success ? int.Parse(matchUtilization.Groups[1].Value) : 0,
            MemoryUsed = matchMemoryUsed.Success ? int.Parse(matchMemoryUsed.Groups[1].Value) : 0,
            MemoryTotal = matchMemoryUsed.Success ? int.Parse(matchMemoryUsed.Groups[2].Value) : 0
        };
    }

public static void GetAMDGPUInfo()
{
    ManagementObjectSearcher searcher = new ManagementObjectSearcher("root\\CIMV2", "SELECT * FROM Win32_VideoController");

    foreach (ManagementObject queryObj in searcher.Get())
    {
        string name = queryObj["Name"].ToString();
        if (name.ToLower().Contains("amd") && name.ToLower().Contains("integrated"))
        {
            // Get GPU utilization
            ManagementObjectSearcher searcherUtilization = new ManagementObjectSearcher("root\\WMI", "SELECT * FROM AMD_Video_Memory_Usage");
            foreach (ManagementObject utilizationObj in searcherUtilization.Get())
            {
                uint utilization = (uint)utilizationObj["CurrentUsage"];
                Console.WriteLine("AMD integrated GPU utilization: " + utilization + "%");
            }

            // Get GPU memory usage
            ManagementObjectSearcher searcherMemory = new ManagementObjectSearcher("root\\CIMV2", "SELECT * FROM CIM_VideoController");
            foreach (ManagementObject memoryObj in searcherMemory.Get())
            {
                string memoryType = memoryObj["VideoProcessor"].ToString();
                if (memoryType.ToLower().Contains("amd"))
                {
                    ulong memorySize = (ulong)memoryObj["AdapterRAM"];
                    Console.WriteLine("AMD integrated GPU memory consumption: " + memorySize / (1024 * 1024) + " MB");
                }
            }
        }
    }
}


}
//PerformanceCounter ramCounter = new PerformanceCounter("Memory", "Available Bytes");
//long availableBytes = Convert.ToInt64(ramCounter.NextValue());
//double availableMegabytes = availableBytes / (1024 * 1024);
//Console.WriteLine("Available RAM: {0} MB", availableMegabytes);
