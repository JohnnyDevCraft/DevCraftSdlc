namespace DevCraft.Cli;

public static class DevCraftInstaller
{
    public static DevCraftInstallationResult Install(string projectDirectory, ProjectProfile? projectProfile = null)
    {
        ProjectProfile profile = ResolveProjectProfile(projectDirectory, projectProfile);
        string controlDirectory = Path.Combine(projectDirectory, ".devcraft");
        List<string> createdPaths = [];
        List<string> preservedPaths = [];

        CreateDirectory(controlDirectory, projectDirectory, createdPaths, preservedPaths);
        CreateDirectory(Path.Combine(controlDirectory, "features"), projectDirectory, createdPaths, preservedPaths);

        WriteIfMissing(Path.Combine(projectDirectory, "AGENT.md"), RootAgent(profile), projectDirectory, createdPaths, preservedPaths);
        WriteIfMissing(Path.Combine(controlDirectory, "AGENT.md"), ControlAgent(profile), projectDirectory, createdPaths, preservedPaths);
        WriteIfMissing(Path.Combine(controlDirectory, "README.md"), Readme(profile), projectDirectory, createdPaths, preservedPaths);
        WriteIfMissing(Path.Combine(controlDirectory, "DISCOVERY.md"), Discovery(profile), projectDirectory, createdPaths, preservedPaths);
        WriteIfMissing(Path.Combine(controlDirectory, "ARCH.md"), Architecture(), projectDirectory, createdPaths, preservedPaths);
        WriteIfMissing(Path.Combine(controlDirectory, "GOV.md"), Governance(), projectDirectory, createdPaths, preservedPaths);
        WriteIfMissing(Path.Combine(controlDirectory, "MODEL.md"), Model(), projectDirectory, createdPaths, preservedPaths);
        WriteIfMissing(Path.Combine(controlDirectory, "THEME.md"), Theme(), projectDirectory, createdPaths, preservedPaths);
        WriteProjectConfiguration(controlDirectory, profile, projectDirectory, createdPaths, preservedPaths);

        return new DevCraftInstallationResult(
            RelativePath(projectDirectory, controlDirectory),
            createdPaths,
            preservedPaths);
    }

    private static void WriteProjectConfiguration(
        string controlDirectory,
        ProjectProfile profile,
        string projectDirectory,
        List<string> createdPaths,
        List<string> preservedPaths)
    {
        string path = Path.Combine(controlDirectory, "configure.json");

        if (File.Exists(path))
        {
            preservedPaths.Add(RelativePath(projectDirectory, path));
            return;
        }

        ProjectDevCraftConfigurationWriter.WriteIfMissing(controlDirectory, profile);
        createdPaths.Add(RelativePath(projectDirectory, path));
    }

    private static ProjectProfile ResolveProjectProfile(string projectDirectory, ProjectProfile? projectProfile)
    {
        if (projectProfile is not null)
        {
            return projectProfile;
        }

        string projectName = Path.GetFileName(Path.GetFullPath(projectDirectory).TrimEnd(Path.DirectorySeparatorChar));

        return new ProjectProfile(projectName, "Pending.", "Pending.");
    }

    private static void CreateDirectory(
        string path,
        string projectDirectory,
        List<string> createdPaths,
        List<string> preservedPaths)
    {
        if (Directory.Exists(path))
        {
            preservedPaths.Add(RelativePath(projectDirectory, path));
            return;
        }

        Directory.CreateDirectory(path);
        createdPaths.Add(RelativePath(projectDirectory, path));
    }

    private static void WriteIfMissing(
        string path,
        string content,
        string projectDirectory,
        List<string> createdPaths,
        List<string> preservedPaths)
    {
        if (File.Exists(path))
        {
            preservedPaths.Add(RelativePath(projectDirectory, path));
            return;
        }

        File.WriteAllText(path, content);
        createdPaths.Add(RelativePath(projectDirectory, path));
    }

    private static string RelativePath(string root, string path)
    {
        return Path.GetRelativePath(root, path).Replace(Path.DirectorySeparatorChar, '/');
    }

    private static string RootAgent(ProjectProfile profile)
    {
        return $"""
            # AGENT.md

            This project uses DevCraft.

            ## Project Context

            - Active Mode: `DevCraft`
            - DevCraft Control Folder: `.devcraft/`
            - Project Name: {profile.Name}
            - Project Purpose: {profile.Purpose}

            ## Working Agreement

            Read `.devcraft/AGENT.md` before making project decisions.
            Keep DevCraft context files current as work progresses.

            """;
    }

    private static string ControlAgent(ProjectProfile profile)
    {
        return $"""
            # AGENT.md

            ## Project Context

            - Active Mode: `DevCraft`
            - DevCraft Control Folder: `.devcraft/`
            - Project Name: {profile.Name}
            - Primary Goal: {profile.Purpose}
            - Project Status: Discovery
            - Valid Project Statuses: Discovery | Active | Maintenance

            ## Work Management

            - Work Item Store: DevCraft artifacts under `.devcraft/`
            - Work Item Tooling: DevCraft

            ## Working Agreements

            - DevCraft control files live under `.devcraft/`.
            - New projects begin in project status `Discovery`.
            - Every DevCraft feature starts with `spec.md`.
            - Features may only move to the next state when the operator explicitly asks.
            - Code is only allowed while the active feature state is `Implementation`.
            - Context files must be updated as part of completed work.

            """;
    }

    private static string Readme(ProjectProfile profile)
    {
        return $"""
            # {profile.Name}

            ## Overview

            {profile.Description}

            ## Goals

            - {profile.Purpose}
            - Keep project context explicit with DevCraft.
            - Track feature intent, plans, implementation, and results.

            """;
    }

    private static string Discovery(ProjectProfile profile)
    {
        return $"""
            # Discovery

            ## Discovery Status

            - Project Status: Discovery
            - Discovery Stage: Brainstorming
            - Valid Discovery Stages: Brainstorming | Architecture Planning | Model Design | Brand and Theming | Governance Design | Complete

            ## MVP Definition

            - Core project purpose: {profile.Purpose}
            - Primary problem solved: Pending.
            - MVP outcome: Pending.

            ## Project Description

            {profile.Description}

            ## Ubiquitous Language

            - Term: Operator
            - Meaning: The human decision-maker who controls DevCraft state transitions and implementation approval.

            """;
    }

    private static string Architecture()
    {
        return """
            # Architecture

            ## Purpose

            Track the chosen architecture and important design decisions for this project.

            ## Selected Architecture

            - Primary Architecture: Undecided during project discovery

            ## Notes

            Architecture will be refined as discovery progresses.

            """;
    }

    private static string Governance()
    {
        return """
            # Governance

            ## Purpose

            Track rules, controls, approvals, and operational boundaries for this project.

            ## AI Governance

            - Implementation starts only after the operator approves the feature for implementation.
            - DevCraft files are part of the auditable project record.

            """;
    }

    private static string Model()
    {
        return """
            # Data Model

            ## Purpose

            Track the current model structure for this project.

            ## Notes

            The model will be refined during project discovery.

            """;
    }

    private static string Theme()
    {
        return """
            # Theme

            ## Purpose

            Track visual theme, design choices, and reusable theme assets for this project.

            ## Notes

            Theme decisions are pending.

            """;
    }
}
