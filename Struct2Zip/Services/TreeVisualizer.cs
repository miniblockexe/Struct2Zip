using System.Text;
using Struct2Zip.Models;

namespace Struct2Zip.Services
{

    /// <summary>
    /// Chuyển flat List&lt;PathEntry&gt; → chuỗi tree text với ký tự vẽ cây Unicode.
    /// Thuật toán: build cây node trong memory → DFS render.
    /// </summary>
    public class TreeVisualizer : ITreeVisualizer
    {
        // ── Internal node tree (chỉ dùng bên trong Visualize) ────────────────
        private class TreeNode
        {
            public string Name { get; }
            public bool IsDirectory { get; set; }

            // Children dùng List để GIỮ thứ tự xuất hiện trong input.
            public List<TreeNode> Children { get; } = new();
            private Dictionary<string, TreeNode> _index = new(StringComparer.Ordinal);

            public TreeNode(string name) => Name = name;

            /// <summary>Lấy hoặc tạo child node, update IsDirectory nếu cần.</summary>
            public TreeNode GetOrAdd(string name, bool isDirectory)
            {
                if (!_index.TryGetValue(name, out var node))
                {
                    node = new TreeNode(name) { IsDirectory = isDirectory };
                    Children.Add(node);
                    _index[name] = node;
                }
                else if (isDirectory)
                    node.IsDirectory = true; // upgrade: intermediate path segment → dir
                return node;
            }
        }

        public string Visualize(List<PathEntry> entries)
        {
            if (entries.Count == 0) return string.Empty;

            // 1. Build cây từ flat paths
            var root = new TreeNode(string.Empty);
            foreach (var entry in entries)
            {
                var parts = entry.RelativePath.Split('/');
                var current = root;
                for (int i = 0; i < parts.Length; i++)
                {
                    // Node trung gian (i < last) luôn là dir dù PathEntry không khai báo
                    bool nodeIsDir = (i < parts.Length - 1) || entry.IsDirectory;
                    current = current.GetOrAdd(parts[i], nodeIsDir);
                }
            }

            // 2. DFS render với ký tự cây
            var sb = new StringBuilder();
            Render(root.Children, sb, prefix: "", isTopLevel: true);
            return sb.ToString().TrimEnd('\r', '\n');
        }

        private static void Render(List<TreeNode> nodes, StringBuilder sb,
                                    string prefix, bool isTopLevel)
        {
            for (int i = 0; i < nodes.Count; i++)
            {
                var node = nodes[i];
                bool isLast = i == nodes.Count - 1;
                string label = node.Name + (node.IsDirectory ? "/" : "");

                if (isTopLevel && nodes.Count == 1)
                {
                    // Single root → không có connector (match AI tree format)
                    sb.AppendLine(label);
                    Render(node.Children, sb, "", false);
                }
                else
                {
                    // ├── hoặc └── tuỳ vị trí, childPrefix giữ alignment cho con
                    string connector = isLast ? "└── " : "├── ";
                    string childPrefix = isLast ? "    " : "│   ";
                    sb.AppendLine(prefix + connector + label);
                    Render(node.Children, sb, prefix + childPrefix, false);
                }
            }
        }
    }
}
