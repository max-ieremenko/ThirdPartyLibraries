using System;
using System.Collections.Generic;
using System.Text;
using ThirdPartyLibraries.Shared;

namespace ThirdPartyLibraries.Suite.Internal.NameCombiners;

internal sealed class NameCombiner
{
    private readonly Dictionary<NamesGroup, string> _fileNameByGroup = new();

    public void Initialize(IEnumerable<NamesGroup> groups)
    {
        var rest = new List<NamesGroup>();

        foreach (var group in groups)
        {
            if (!group.PreferredFileName.IsNullOrEmpty())
            {
                var fileName = group.PreferredFileName + group.FileExtension;
                _fileNameByGroup.Add(group, fileName);
            }
            else
            {
                rest.Add(group);
            }
        }

        while (rest.Count > 0 && ProcessByNames(rest, false))
        {
        }

        while (rest.Count > 0 && ProcessByNames(rest, true))
        {
        }

        while (rest.Count > 0)
        {
            ProcessByGroupName(rest);
        }
    }

    public string GetFileName(NamesGroup group)
    {
        return _fileNameByGroup[group];
    }

    private bool ProcessByNames(List<NamesGroup> rest, bool useAlternative)
    {
        var candidates = new List<NamesGroup>();
        string candidateName = null;
        for (var i = 0; i < rest.Count; i++)
        {
            var group = rest[i];
            var groupName = TryGetFileNameByNames(group, useAlternative, false);
            if (groupName == null)
            {
                continue;
            }

            if (candidateName == null)
            {
                candidateName = groupName;
            }

            if (candidateName.EqualsIgnoreCase(groupName))
            {
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
            _fileNameByGroup.Add(candidates[0], candidateName);
            return true;
        }

        candidateName = TryGetFileNameByNames(candidates[0], useAlternative, true);
        var isUnique = true;
        for (var i = 1; i < candidates.Count; i++)
        {
            var group = candidates[i];
            var groupName = TryGetFileNameByNames(group, useAlternative, true);
            if (candidateName.EqualsIgnoreCase(groupName))
            {
                isUnique = false;
                break;
            }
        }

        if (isUnique)
        {
            _fileNameByGroup.Add(candidates[0], candidateName);
            for (var i = 1; i < candidates.Count; i++)
            {
                var group = candidates[i];
                var groupName = TryGetFileNameByNames(group, useAlternative, true);
                _fileNameByGroup.Add(group, groupName);
            }

            return true;
        }

        for (var i = 0; i < candidates.Count; i++)
        {
            var group = candidates[i];
            var groupName = TryGetFileNameByNames(group, useAlternative, true, i + 1);
            _fileNameByGroup.Add(group, groupName);
        }

        return true;
    }

    private void ProcessByGroupName(List<NamesGroup> rest)
    {
        var candidates = new List<NamesGroup>();
        string candidateName = null;
        for (var i = 0; i < rest.Count; i++)
        {
            var group = rest[i];
            var groupName = GetFileNameByGroupName(group);

            if (candidateName == null)
            {
                candidateName = groupName;
            }

            if (candidateName.EqualsIgnoreCase(groupName))
            {
                candidates.Add(group);
                rest.RemoveAt(i);
                i--;
            }
        }

        if (candidates.Count == 1)
        {
            _fileNameByGroup.Add(candidates[0], candidateName);
            return;
        }

        for (var i = 0; i < candidates.Count; i++)
        {
            var group = candidates[i];
            var groupName = GetFileNameByGroupName(group, i + 1);
            _fileNameByGroup.Add(group, groupName);
        }
    }

    private string TryGetFileNameByNames(NamesGroup group, bool useAlternative, bool includeSecond, int? index = default)
    {
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
                return null;
            }
        }

        if (source == null)
        {
            return null;
        }

        var fileName = new StringBuilder();
        if (useAlternative)
        {
            fileName.Append(group.GroupName).Append('-');
        }

        fileName.Append(source.First);

        if (includeSecond)
        {
            fileName.Append('-').Append(source.Second);
        }

        if (index.HasValue)
        {
            fileName.Append('-').Append(index.Value);
        }

        fileName.Append(group.FileExtension);
        return fileName.ToString();
    }

    private string GetFileNameByGroupName(NamesGroup group, int? index = default)
    {
        var fileName = new StringBuilder()
            .Append(group.GroupName);

        if (index.HasValue)
        {
            fileName.Append('-').Append(index.Value);
        }

        fileName.Append(group.FileExtension);
        return fileName.ToString();
    }
}