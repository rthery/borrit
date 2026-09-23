using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Text.RegularExpressions;
using System.Threading;
using UnityEngine;

namespace BorritEditor.Database.GitRef
{
    internal class GitRefException : Exception
    {
        public GitRefException(string message) : base(message)
        {
        }
    }

    internal class GitRefSnapshot
    {
        public readonly string Sha;
        public readonly List<InternalDataBase.EntryDTO> Entries;
        public readonly bool HasChanged;

        public GitRefSnapshot(string sha, List<InternalDataBase.EntryDTO> entries, bool hasChanged)
        {
            Sha = sha;
            Entries = entries;
            HasChanged = hasChanged;
        }
    }

    internal class GitRefStore
    {
        private const string LocksFileName = "locks.json";
        private const string FallbackEmail = "borrit@localhost";
        private const int MaxUpdateAttempts = 10;
        private const int MinRetryDelayMilliseconds = 100;
        private const int MaxRetryDelayMilliseconds = 1000;
        private static readonly Version MinimumGitVersion = new Version(2, 29);
        private static readonly Regex RemoteNameRegex = new Regex(@"^[A-Za-z0-9][A-Za-z0-9._/-]*$");
        private static readonly Regex RefNameRegex = new Regex(@"^refs/[A-Za-z0-9._/-]+$");
        private static readonly Regex ShaRegex = new Regex(@"^[0-9a-f]{40}([0-9a-f]{24})?$");

        [Serializable]
        private class LocksFile
        {
            public List<InternalDataBase.EntryDTO> Entries = new List<InternalDataBase.EntryDTO>();
        }

        private readonly string _workingDirectory;
        private readonly string _remote;
        private readonly string _refName;

        private readonly System.Random _random = new System.Random();

        private string _cachedSha;
        private List<InternalDataBase.EntryDTO> _cachedEntries = new List<InternalDataBase.EntryDTO>();

        public GitRefStore(string workingDirectory, string remote, string refName)
        {
            _workingDirectory = workingDirectory;
            _remote = remote;
            _refName = refName;
        }

        public string CheckSetup()
        {
            GitResult versionResult;
            try
            {
                versionResult = Run("--version");
            }
            catch (Win32Exception)
            {
                return "Git is not installed or could not be found in PATH";
            }

            if (versionResult.Success == false)
                return $"Git is not working properly: {versionResult.Error.Trim()}";

            Match versionMatch = Regex.Match(versionResult.Output, @"(\d+)\.(\d+)");
            if (versionMatch.Success == false)
                return $"Could not read git version from '{versionResult.Output.Trim()}'";

            Version gitVersion = new Version(int.Parse(versionMatch.Groups[1].Value), int.Parse(versionMatch.Groups[2].Value));
            if (gitVersion < MinimumGitVersion)
                return $"Git {MinimumGitVersion} or newer is required, found {gitVersion}";

            if (Run("rev-parse --is-inside-work-tree").Success == false)
                return "The project is not inside a git repository";

            if (_remote == null || RemoteNameRegex.IsMatch(_remote) == false || Run($"remote get-url \"{_remote}\"").Success == false)
                return $"Git remote '{_remote}' does not exist";

            if (_refName == null || RefNameRegex.IsMatch(_refName) == false || Run($"check-ref-format \"{_refName}\"").Success == false)
                return $"'{_refName}' is not a valid ref name, it must start with refs/ (e.g. refs/borrit/locks)";

            return null;
        }

        public GitRefSnapshot Fetch()
        {
            GitResult lsRemoteResult = Run($"ls-remote \"{_remote}\" \"{_refName}\"");
            if (lsRemoteResult.Success == false)
                throw new GitRefException($"Failed to reach git remote '{_remote}': {lsRemoteResult.Error.Trim()}");

            string remoteSha = ParseLsRemoteSha(lsRemoteResult.Output);
            if (remoteSha == _cachedSha)
                return new GitRefSnapshot(_cachedSha, _cachedEntries, false);

            List<InternalDataBase.EntryDTO> entries = new List<InternalDataBase.EntryDTO>();
            if (remoteSha != null)
            {
                if (Run($"cat-file -e {remoteSha}^{{commit}}").Success == false)
                {
                    GitResult fetchResult = Run($"fetch --quiet --no-tags --no-write-fetch-head \"{_remote}\" \"+{_refName}:{_refName}\"");
                    if (fetchResult.Success == false)
                        throw new GitRefException($"Failed to fetch {_refName} from '{_remote}': {fetchResult.Error.Trim()}");
                }

                GitResult readResult = Run($"cat-file blob {remoteSha}:{LocksFileName}");
                if (readResult.Success == false)
                    throw new GitRefException($"Failed to read {LocksFileName} from {_refName}: {readResult.Error.Trim()}");

                try
                {
                    LocksFile locksFile = JsonUtility.FromJson<LocksFile>(readResult.Output);
                    if (locksFile != null && locksFile.Entries != null)
                        entries = locksFile.Entries;
                }
                catch (Exception e)
                {
                    throw new GitRefException($"Failed to parse {LocksFileName} from {_refName}: {e.Message}");
                }
            }

            _cachedSha = remoteSha;
            _cachedEntries = entries;
            return new GitRefSnapshot(_cachedSha, _cachedEntries, true);
        }

        public GitRefSnapshot Update(Func<List<InternalDataBase.EntryDTO>, bool> applyChanges, string authorName)
        {
            for (int attempt = 0; attempt < MaxUpdateAttempts; attempt++)
            {
                GitRefSnapshot snapshot = Fetch();
                List<InternalDataBase.EntryDTO> entries = new List<InternalDataBase.EntryDTO>(snapshot.Entries);
                if (applyChanges(entries) == false)
                    return snapshot;

                string commitSha = WriteCommit(entries, snapshot.Sha, authorName);
                GitResult pushResult = Run($"push --no-verify --porcelain \"{_remote}\" \"{commitSha}:{_refName}\"");
                if (pushResult.Success)
                {
                    Run($"update-ref \"{_refName}\" {commitSha}");
                    _cachedSha = commitSha;
                    _cachedEntries = entries;
                    return new GitRefSnapshot(_cachedSha, _cachedEntries, true);
                }

                if (HasRemoteMovedFrom(snapshot.Sha) == false)
                    throw new GitRefException($"Failed to push {_refName} to '{_remote}': {(pushResult.Error + pushResult.Output).Trim()}");

                Thread.Sleep(_random.Next(MinRetryDelayMilliseconds, MaxRetryDelayMilliseconds));
            }

            throw new GitRefException($"Failed to update {_refName}, too many concurrent updates. Please try again.");
        }

        private string WriteCommit(List<InternalDataBase.EntryDTO> entries, string parentSha, string authorName)
        {
            string json = JsonUtility.ToJson(new LocksFile { Entries = entries }, true);
            string blobSha = RunOrThrow("hash-object -w --stdin", json);
            string treeSha = RunOrThrow("mktree", $"100644 blob {blobSha}\t{LocksFileName}\n");

            Dictionary<string, string> environment = new Dictionary<string, string>
            {
                { "GIT_AUTHOR_NAME", authorName },
                { "GIT_COMMITTER_NAME", authorName },
                { "EMAIL", FallbackEmail }
            };
            string parentArgument = parentSha != null ? $" -p {parentSha}" : string.Empty;
            return RunOrThrow($"commit-tree --no-gpg-sign {treeSha}{parentArgument} -m \"Update borrowed assets\"", null, environment);
        }

        private string ParseLsRemoteSha(string output)
        {
            foreach (string line in output.Split('\n'))
            {
                string[] columns = line.Trim().Split('\t');
                if (columns.Length == 2 && columns[1] == _refName && ShaRegex.IsMatch(columns[0]))
                    return columns[0];
            }

            return null;
        }

        private bool HasRemoteMovedFrom(string sha)
        {
            GitResult lsRemoteResult = Run($"ls-remote \"{_remote}\" \"{_refName}\"");
            return lsRemoteResult.Success && ParseLsRemoteSha(lsRemoteResult.Output) != sha;
        }

        private string RunOrThrow(string arguments, string input = null, IDictionary<string, string> environment = null)
        {
            GitResult result = Run(arguments, input, environment);
            if (result.Success == false)
                throw new GitRefException($"git {arguments} failed: {result.Error.Trim()}");

            return result.Output.Trim();
        }

        private GitResult Run(string arguments, string input = null, IDictionary<string, string> environment = null)
        {
            return GitProcess.Run(_workingDirectory, arguments, input, environment);
        }
    }
}
