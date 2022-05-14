using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using ThirdPartyLibraries.Shared;

namespace ThirdPartyLibraries.Suite.Internal.NameCombiners;

internal sealed class NameCombiner
{
    private readonly Dictionary<NamesGroup, string> _fileNameByGroup = new();

    public void Initialize(IEnumerable<NamesGroup> groups)
    {
        var rest = new List<NamesGroup>();
        var names = new List<GroupFileName>();
        var uniqueFileNames = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        foreach (var group in groups)
        {
            if (group.PreferredFileName.IsNullOrEmpty())
            {
                rest.Add(group);
            }
            else
            {
                names.Add(new GroupFileName(group, group.PreferredFileName, group.FileExtension, -1));
            }
        }

        AddFileNames(names, uniqueFileNames);
        names.Clear();

        while (rest.Count > 0 && ProcessByNames(rest, false, names))
        {
        }

        while (rest.Count > 0 && ProcessByNames(rest, true, names))
        {
        }

        for (var i = 0; i < rest.Count; i++)
        {
            var group = rest[i];
            names.Add(new GroupFileName(group, group.GroupName, group.FileExtension, 3));
        }

        AddFileNames(names, uniqueFileNames);
    }

    public string GetFileName(NamesGroup group)
    {
        return _fileNameByGroup[group];
    }

    private static bool TryGetFileNameByNames(NamesGroup group, bool useAlternative, bool includeSecond, out FileName fileName)
    {
        fileName = default;
        var names = useAlternative ? group.AlternativeNames : group.Names;

        Name source = null;
        foreach (var name in names)
        {
            if (source == null)
            {
                source = name;
                continue;
            }

            if (!name.First.StartsWith(source.First, StringComparison.OrdinalIgnoreCase))
            {
                return false;
            }
        }

        if (source == null)
        {
            return false;
        }

        fileName = new FileName(
            useAlternative ? group.GroupName : null,
            source.First,
            includeSecond ? source.Second : null,
            group.FileExtension);
        return true;
    }

    private static bool ProcessByNames(List<NamesGroup> rest, bool useAlternative, List<GroupFileName> destination)
    {
        var candidates = new List<NamesGroup>();

        FileName candidateName = default;
        for (var i = 0; i < rest.Count; i++)
        {
            var group = rest[i];
            if (!TryGetFileNameByNames(group, useAlternative, false, out var groupName))
            {
                continue;
            }

            if (candidates.Count == 0 || candidateName.Equals(groupName))
            {
                candidateName = groupName;
                candidates.Add(group);
                rest.RemoveAt(i);
                i--;
            }
        }

        if (candidates.Count == 0)
        {
            return false;
        }

        if (candidates.Count == 1)
        {
            destination.Add(new GroupFileName(candidates[0], candidateName.GetFileName(), candidateName.Extension, -1));
            return true;
        }

        for (var i = 0; i < candidates.Count; i++)
        {
            var group = candidates[i];
            TryGetFileNameByNames(group, useAlternative, true, out var groupName);

            destination.Add(new GroupFileName(group, groupName.GetFileName(), groupName.Extension, -1));
        }

        return true;
    }

    private static string BuildFileName(byte[] hash, string name, string extension, int hashIndex)
    {
        if (hashIndex < 0)
        {
            return name + extension;
        }

        var result = new StringBuilder(name)
            .Append('_');
        for (var i = 0; i <= hashIndex; i++)
        {
            result.Append(hash[i].ToString("x2"));
        }

        result.Append(extension);
        return result.ToString();
    }

    private void AddFileNames(List<GroupFileName> groups, HashSet<string> uniqueFileNames)
    {
        var byFileName = groups.GroupBy(
            i => BuildFileName(i.Group.GroupHash, i.FileName, i.Extension, i.StartHashIndex),
            i => i,
            StringComparer.OrdinalIgnoreCase);

        foreach (var entry in byFileName)
        {
            var entryFiles = entry.ToArray();

            if (!uniqueFileNames.Contains(entry.Key) && entryFiles.Length == 1)
            {
                _fileNameByGroup.Add(entryFiles[0].Group, entry.Key);
                uniqueFileNames.Add(entry.Key);
            }
            else
            {
                AddFileNamesWithHash(entryFiles, uniqueFileNames);
            }
        }
    }

    private void AddFileNamesWithHash(GroupFileName[] entries, HashSet<string> uniqueFileNames)
    {
        var fileNames = new string[entries.Length];

        bool isUnique;
        var hashIndexOffset = 1;
        do
        {
            isUnique = true;
            for (var i = 0; i < entries.Length; i++)
            {
                var entry = entries[i];
                var fileName = BuildFileName(entry.Group.GroupHash, entry.FileName, entry.Extension, entry.StartHashIndex + hashIndexOffset);
                fileNames[i] = fileName;

                if (uniqueFileNames.Contains(fileName) || (i > 0 && fileNames[0].EqualsIgnoreCase(fileName)))
                {
                    isUnique = false;
                    break;
                }
            }

            hashIndexOffset++;
        }
        while (!isUnique);

        for (var i = 0; i < entries.Length; i++)
        {
            _fileNameByGroup.Add(entries[i].Group, fileNames[i]);
        }
    }

    private readonly struct FileName
    {
        private readonly string _groupName;
        private readonly string _first;
        private readonly string _second;

        public FileName(string groupName, string first, string second, string extension)
        {
            _groupName = groupName;
            _first = first;
            _second = second;
            Extension = extension;
        }

        public string Extension { get; }

        public bool Equals(in FileName other)
        {
            return StringComparer.OrdinalIgnoreCase.Equals(_groupName, other._groupName)
                   && StringComparer.OrdinalIgnoreCase.Equals(_first, other._first)
                   && StringComparer.OrdinalIgnoreCase.Equals(_second, other._second)
                   && StringComparer.OrdinalIgnoreCase.Equals(Extension, other.Extension);
        }

        public string GetFileName()
        {
            var result = new StringBuilder(_groupName);

            if (!_first.IsNullOrEmpty())
            {
                if (result.Length > 0)
                {
                    result.Append('-');
                }

                result.Append(_first);
            }

            if (!_second.IsNullOrEmpty())
            {
                if (result.Length > 0)
                {
                    result.Append('-');
                }

                result.Append(_second);
            }

            return result.ToString();
        }
    }

    private sealed class GroupFileName
    {
        public GroupFileName(NamesGroup group, string fileName, string extension, int startHashIndex)
        {
            Group = group;
            FileName = fileName;
            Extension = extension;
            StartHashIndex = startHashIndex;
        }

        public NamesGroup Group { get; }

        public string FileName { get; }

        public string Extension { get; }

        public int StartHashIndex { get; }
    }
}