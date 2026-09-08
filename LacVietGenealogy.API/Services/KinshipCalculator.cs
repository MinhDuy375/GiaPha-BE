using LacVietGenealogy.Core.Entities;

namespace LacVietGenealogy.API.Services
{
    /// <summary>
    /// Thuật toán tính danh xưng theo huyết thống Việt Nam.
    /// Tham khảo logic từ giapha-os (github.com/homielab/giapha-os).
    /// </summary>
    public static class KinshipCalculator
    {
        // Bảng danh xưng trực hệ vế trên
        private static readonly string[] Ancestors = ["", "Bố/Mẹ", "Ông/Bà", "Cụ", "Kỵ", "Sơ", "Tiệm", "Tiểu", "Di", "Diễn"];
        // Bảng danh xưng trực hệ vế dưới  
        private static readonly string[] Descendants = ["", "Con", "Cháu", "Chắt", "Chít", "Chút", "Chét", "Chót", "Chẹt"];

        public class KinshipResult
        {
            public string ACallsB { get; set; } = "Không rõ";
            public string BCallsA { get; set; } = "Không rõ";
            public string Description { get; set; } = "";
            public int Distance { get; set; } = 0;
            public List<string> PathLabels { get; set; } = new();
        }

        private class PersonNode
        {
            public Guid Id { get; set; }
            public string FullName { get; set; } = "";
            public int Gender { get; set; } // 0=male, 1=female
            public int? BirthYear { get; set; }
            public int GenerationLevel { get; set; }
        }

        public static KinshipResult Calculate(
            Guid fromId,
            Guid toId,
            List<Member> members,
            List<ParentChildRelationship> parentChildRels,
            List<SpouseRelationship> spouseRels)
        {
            if (fromId == toId)
                return new KinshipResult
                {
                    ACallsB = "Chính mình",
                    BCallsA = "Chính mình",
                    Description = "Cùng một người"
                };

            // Kiểm tra quan hệ vợ chồng trực tiếp
            var isSpouse = spouseRels.Any(s =>
                (s.HusbandId == fromId && s.WifeId == toId) ||
                (s.HusbandId == toId && s.WifeId == fromId));

            if (isSpouse)
            {
                var fromMember = members.FirstOrDefault(m => m.Id == fromId);
                if (fromMember?.Gender == 0) // Nam gọi vợ
                    return new KinshipResult { ACallsB = "Vợ", BCallsA = "Chồng", Description = "Quan hệ vợ chồng", Distance = 1, PathLabels = new() { "Hai người là vợ chồng" } };
                else
                    return new KinshipResult { ACallsB = "Chồng", BCallsA = "Vợ", Description = "Quan hệ vợ chồng", Distance = 1, PathLabels = new() { "Hai người là vợ chồng" } };
            }

            // Xây dựng map thành viên
            var personMap = members.ToDictionary(m => m.Id, m => new PersonNode
            {
                Id = m.Id,
                FullName = m.FullName,
                Gender = m.Gender,
                BirthYear = m.BirthDateSolar?.Year,
                GenerationLevel = m.GenerationLevel
            });

            // Xây dựng map cha-mẹ: childId -> list of parentIds
            var parentsOf = new Dictionary<Guid, List<Guid>>();
            // Xây dựng map con cái: parentId -> list of childIds
            var childrenOf = new Dictionary<Guid, List<Guid>>();

            foreach (var rel in parentChildRels)
            {
                if (!parentsOf.ContainsKey(rel.ChildId)) parentsOf[rel.ChildId] = new();
                parentsOf[rel.ChildId].Add(rel.ParentId);

                if (!childrenOf.ContainsKey(rel.ParentId)) childrenOf[rel.ParentId] = new();
                childrenOf[rel.ParentId].Add(rel.ChildId);
            }

            // BFS để tìm LCA (Lowest Common Ancestor)
            // pathFromA: bản đồ mỗi ancestor -> đường đi từ fromId lên
            var pathFromA = FindAncestors(fromId, parentsOf);
            var pathFromB = FindAncestors(toId, parentsOf);

            // Tìm LCA là ancestor chung gần nhất
            Guid? lca = null;
            int depthA = 0, depthB = 0;
            List<Guid> pathAToLca = new(), pathBToLca = new();

            foreach (var (ancestor, pathA) in pathFromA.OrderBy(p => p.Value.Count))
            {
                if (pathFromB.TryGetValue(ancestor, out var pathB))
                {
                    lca = ancestor;
                    depthA = pathA.Count;
                    depthB = pathB.Count;
                    pathAToLca = pathA;
                    pathBToLca = pathB;
                    break;
                }
            }

            if (lca == null)
                return new KinshipResult { ACallsB = "Họ hàng xa", BCallsA = "Họ hàng xa", Description = "Không tìm thấy mối quan hệ", Distance = 0 };

            var personA = personMap.TryGetValue(fromId, out var pA) ? pA : null;
            var personB = personMap.TryGetValue(toId, out var pB) ? pB : null;

            if (personA == null || personB == null)
                return new KinshipResult
                {
                    ACallsB = "Không rõ",
                    BCallsA = "Không rõ",
                    Description = "Không tìm thấy thành viên"
                };

            var spousesOfA = spouseRels
                .Where(rel => rel.HusbandId == fromId || rel.WifeId == fromId)
                .Select(rel => rel.HusbandId == fromId ? rel.WifeId : rel.HusbandId)
                .Distinct()
                .ToList();
            foreach (var spouseId in spousesOfA.Where(id => id != toId))
            {
                var bloodResult = Calculate(spouseId, toId, members, parentChildRels, new List<SpouseRelationship>());
                if (IsResolvedBloodResult(bloodResult))
                {
                    return new KinshipResult
                    {
                        ACallsB = AddSpouseSuffix(bloodResult.ACallsB, personA.Gender),
                        BCallsA = ToInLawTerm(bloodResult.BCallsA, personA.Gender),
                        Description = $"Thông qua hôn nhân của {personMap[spouseId].FullName}",
                        Distance = bloodResult.Distance + 1,
                        PathLabels = new List<string> { $"{personA.FullName} là {(personA.Gender == 0 ? "chồng" : "vợ")} của {personMap[spouseId].FullName}" }
                            .Concat(bloodResult.PathLabels)
                            .ToList()
                    };
                }
            }

            var spousesOfB = spouseRels
                .Where(rel => rel.HusbandId == toId || rel.WifeId == toId)
                .Select(rel => rel.HusbandId == toId ? rel.WifeId : rel.HusbandId)
                .Distinct()
                .ToList();
            foreach (var spouseId in spousesOfB.Where(id => id != fromId))
            {
                var bloodResult = Calculate(fromId, spouseId, members, parentChildRels, new List<SpouseRelationship>());
                if (IsResolvedBloodResult(bloodResult))
                {
                    return new KinshipResult
                    {
                        ACallsB = ToInLawTerm(bloodResult.ACallsB, personB.Gender),
                        BCallsA = AddSpouseSuffix(bloodResult.BCallsA, personB.Gender),
                        Description = $"Thông qua hôn nhân của {personMap[spouseId].FullName}",
                        Distance = bloodResult.Distance + 1,
                        PathLabels = bloodResult.PathLabels
                            .Concat(new[] { $"{personB.FullName} là {(personB.Gender == 0 ? "chồng" : "vợ")} của {personMap[spouseId].FullName}" })
                            .ToList()
                    };
                }
            }

            return ResolveKinship(depthA, depthB, personA, personB, pathAToLca, pathBToLca, personMap);
        }

        private static bool IsResolvedBloodResult(KinshipResult result)
        {
            return result.Description != "Không tìm thấy mối quan hệ" && result.Description != "Không tìm thấy thành viên";
        }

        private static string AddSpouseSuffix(string term, int spouseGender)
        {
            if (term is "Bố" or "Mẹ" || term.StartsWith("Ông") || term.StartsWith("Bà") || term.StartsWith("Cụ"))
                return $"{term} {(spouseGender == 0 ? "vợ" : "chồng")}";

            if (term.Contains("Anh")) return $"Anh {(spouseGender == 0 ? "vợ" : "chồng")}";
            if (term.Contains("Chị")) return $"Chị {(spouseGender == 0 ? "vợ" : "chồng")}";
            if (term.Contains("Em")) return $"Em {(spouseGender == 0 ? "vợ" : "chồng")}";
            return $"{term} {(spouseGender == 0 ? "vợ" : "chồng")}";
        }

        private static string ToInLawTerm(string term, int personGender)
        {
            if (term == "Con" || term == "Cháu") return $"{term} {(personGender == 0 ? "rể" : "dâu")}";
            if (term.Contains("Anh")) return personGender == 0 ? "Anh rể" : "Anh";
            if (term.Contains("Chị")) return personGender == 0 ? "Chị" : "Chị dâu";
            if (term.Contains("Em")) return personGender == 0 ? "Em rể" : "Em dâu";
            return term;
        }

        private static Dictionary<Guid, List<Guid>> FindAncestors(Guid startId, Dictionary<Guid, List<Guid>> parentsOf)
        {
            // BFS từ startId lên trên, trả về map: ancestorId -> đường đi (list các bước, không gồm startId, gồm ancestorId)
            var visited = new Dictionary<Guid, List<Guid>>();
            var queue = new Queue<(Guid nodeId, List<Guid> path)>();
            visited[startId] = new List<Guid>(); // startId -> chính nó (depth=0)
            queue.Enqueue((startId, new List<Guid>()));

            while (queue.Count > 0)
            {
                var (current, path) = queue.Dequeue();
                if (!parentsOf.TryGetValue(current, out var parents)) continue;

                foreach (var parent in parents)
                {
                    if (!visited.ContainsKey(parent))
                    {
                        var newPath = new List<Guid>(path) { parent };
                        visited[parent] = newPath;
                        queue.Enqueue((parent, newPath));
                    }
                }
            }
            return visited;
        }

        private static KinshipResult ResolveKinship(
            int depthA, int depthB,
            PersonNode? personA, PersonNode? personB,
            List<Guid> pathAToLca, List<Guid> pathBToLca,
            Dictionary<Guid, PersonNode> personMap)
        {
            if (personA == null || personB == null)
                return new KinshipResult { ACallsB = "Không rõ", BCallsA = "Không rõ" };

            string genderB = personB.Gender == 0 ? "male" : "female";
            string genderA = personA.Gender == 0 ? "male" : "female";

            // 1. Trực hệ: A là tổ tiên của B (A chính là LCA)
            if (depthA == 0)
            {
                // Người con đầu tiên của A trên đường xuống B
                var firstChildId = pathBToLca.Count > 0 ? pathBToLca[0] : Guid.Empty;
                bool isPaternal = firstChildId != Guid.Empty && personMap.TryGetValue(firstChildId, out var fc) && fc.Gender == 0;

                var bCallsA = GetAncestorTerm(depthB, genderA, isPaternal);
                var aCallsB = GetDescendantTerm(depthB);
                return new KinshipResult { ACallsB = aCallsB, BCallsA = bCallsA, Description = "Quan hệ Trực hệ", Distance = depthB, PathLabels = BuildPathLabels(pathBToLca, personMap) };
            }

            // 2. Trực hệ: B là tổ tiên của A (B chính là LCA)
            if (depthB == 0)
            {
                var firstChildId = pathAToLca.Count > 0 ? pathAToLca[0] : Guid.Empty;
                bool isPaternal = firstChildId != Guid.Empty && personMap.TryGetValue(firstChildId, out var fc) && fc.Gender == 0;

                var aCallsB = GetAncestorTerm(depthA, genderB, isPaternal);
                var bCallsA = GetDescendantTerm(depthA);
                return new KinshipResult { ACallsB = aCallsB, BCallsA = bCallsA, Description = "Quan hệ Trực hệ", Distance = depthA, PathLabels = BuildPathLabels(pathAToLca, personMap) };
            }

            // 3. Quan hệ ngang hàng
            // Lấy nhánh gốc (con trực tiếp của LCA trên mỗi phía)
            Guid branchAId = pathAToLca.Count > 0 ? pathAToLca[0] : personA.Id;
            Guid branchBId = pathBToLca.Count > 0 ? pathBToLca[0] : personB.Id;

            personMap.TryGetValue(branchAId, out var branchA);
            personMap.TryGetValue(branchBId, out var branchB);

            // Xét thứ bậc theo ngày sinh / thứ bậc sinh
            bool branchAIsSenior = IsSenior(branchA, branchB);

            int diff = depthA - depthB; // diff > 0: B thuộc thế hệ cao hơn
            bool isSameGen = depthA == depthB;

            if (isSameGen && depthA == 1)
            {
                // Anh chị em ruột
                return BuildSiblingResult(genderA, genderB, branchAIsSenior);
            }

            if (isSameGen && depthA == 2)
            {
                // Anh chị em họ (con anh/chị/em)
                return BuildCousinResult(genderA, genderB, branchA, branchB, branchAIsSenior);
            }

            // Quan hệ chú bác, cô, dì...
            if (!isSameGen)
            {
                return BuildUncleAuntResult(depthA, depthB, genderA, genderB, branchA, branchB, branchAIsSenior);
            }

            // Quan hệ xa hơn
            return new KinshipResult
            {
                ACallsB = $"Họ hàng ({depthB} đời)",
                BCallsA = $"Họ hàng ({depthA} đời)",
                Description = $"Họ hàng cách {depthA + depthB} bậc",
                Distance = depthA + depthB
            };
        }

        private static List<string> BuildPathLabels(List<Guid> path, Dictionary<Guid, PersonNode> personMap)
        {
            return path
                .Where(personMap.ContainsKey)
                .Select(id => personMap[id].FullName)
                .ToList();
        }

        private static string GetAncestorTerm(int depth, string gender, bool isPaternal)
        {
            if (depth == 1) return gender == "female" ? "Mẹ" : "Bố";
            if (depth == 2)
            {
                string base2 = gender == "female" ? "Bà" : "Ông";
                return $"{base2} {(isPaternal ? "nội" : "ngoại")}";
            }
            if (depth == 3)
            {
                string base3 = gender == "female" ? "Cụ bà (bà cố)" : "Cụ ông (ông cố)";
                return $"{base3} {(isPaternal ? "nội" : "ngoại")}";
            }
            return depth < Ancestors.Length ? Ancestors[depth] : $"Tổ đời {depth}";
        }

        private static string GetDescendantTerm(int depth)
        {
            return depth < Descendants.Length ? Descendants[depth] : $"Cháu đời {depth}";
        }

        private static bool IsSenior(PersonNode? a, PersonNode? b)
        {
            if (a == null || b == null) return true;
            if (a.GenerationLevel != b.GenerationLevel) return a.GenerationLevel < b.GenerationLevel;
            if (a.BirthYear.HasValue && b.BirthYear.HasValue) return a.BirthYear < b.BirthYear;
            return true;
        }

        private static KinshipResult BuildSiblingResult(string genderA, string genderB, bool aIsSenior)
        {
            if (aIsSenior)
            {
                // A lớn hơn B
                string aCallsB = genderB == "male" ? "Em trai" : "Em gái";
                string bCallsA = genderA == "male" ? "Anh" : "Chị";
                return new KinshipResult { ACallsB = aCallsB, BCallsA = bCallsA, Description = "Anh/Chị Em ruột", Distance = 2 };
            }
            else
            {
                // B lớn hơn A
                string aCallsB = genderB == "male" ? "Anh" : "Chị";
                string bCallsA = genderA == "male" ? "Em trai" : "Em gái";
                return new KinshipResult { ACallsB = aCallsB, BCallsA = bCallsA, Description = "Anh/Chị Em ruột", Distance = 2 };
            }
        }

        private static KinshipResult BuildCousinResult(string genderA, string genderB, PersonNode? branchA, PersonNode? branchB, bool branchAIsSenior)
        {
            // Bên nhánh nào lớn hơn quyết định vai trò
            string parentA = branchA?.Gender == 0 ? "anh" : "chị";
            string parentB = branchB?.Gender == 0 ? "anh" : "chị";

            if (branchAIsSenior)
            {
                string aCallsB = genderB == "male" ? "Em họ" : "Em họ";
                string bCallsA = genderA == "male" ? "Anh họ" : "Chị họ";
                return new KinshipResult { ACallsB = aCallsB, BCallsA = bCallsA, Description = "Anh/Chị Em họ", Distance = 4 };
            }
            else
            {
                string aCallsB = genderB == "male" ? "Anh họ" : "Chị họ";
                string bCallsA = genderA == "male" ? "Em họ" : "Em họ";
                return new KinshipResult { ACallsB = aCallsB, BCallsA = bCallsA, Description = "Anh/Chị Em họ", Distance = 4 };
            }
        }

        private static KinshipResult BuildUncleAuntResult(
            int depthA, int depthB,
            string genderA, string genderB,
            PersonNode? branchA, PersonNode? branchB,
            bool branchAIsSenior)
        {
            // depthA < depthB: A thuộc thế hệ trên B  (A là bậc trên của B)
            // depthA > depthB: B thuộc thế hệ trên A
            if (depthA < depthB)
            {
                // A là bậc trên B
                bool aIsPaternal = branchA?.Gender == 0;
                string aCallsB;
                string bCallsA;

                if (depthA == 1 && depthB == 2)
                {
                    // A là chú/bác/cô/dì của B
                    if (branchAIsSenior)
                    {
                        aCallsB = GetDescendantTerm(depthB - depthA); // "Cháu"
                        bCallsA = genderA == "male" ? (aIsPaternal ? "Bác" : "Bác") : (aIsPaternal ? "Cô" : "Dì");
                    }
                    else
                    {
                        aCallsB = GetDescendantTerm(depthB - depthA);
                        bCallsA = genderA == "male" ? (aIsPaternal ? "Chú" : "Cậu") : (aIsPaternal ? "Cô" : "Dì");
                    }
                    return new KinshipResult { ACallsB = aCallsB, BCallsA = bCallsA, Description = "Chú Bác Cô Dì – Cháu", Distance = depthA + depthB };
                }

                aCallsB = GetDescendantTerm(depthB - depthA);
                bCallsA = $"Họ hàng (bậc {depthA})";
                return new KinshipResult { ACallsB = aCallsB, BCallsA = bCallsA, Description = $"Họ hàng cách {depthA + depthB} bậc", Distance = depthA + depthB };
            }
            else
            {
                // B là bậc trên A
                bool bIsPaternal = branchB?.Gender == 0;
                string aCallsB;
                string bCallsA;

                if (depthB == 1 && depthA == 2)
                {
                    if (branchAIsSenior)
                    {
                        bCallsA = GetDescendantTerm(depthA - depthB);
                        aCallsB = genderB == "male" ? (bIsPaternal ? "Bác" : "Bác") : (bIsPaternal ? "Cô" : "Dì");
                    }
                    else
                    {
                        bCallsA = GetDescendantTerm(depthA - depthB);
                        aCallsB = genderB == "male" ? (bIsPaternal ? "Chú" : "Cậu") : (bIsPaternal ? "Cô" : "Dì");
                    }
                    return new KinshipResult { ACallsB = aCallsB, BCallsA = bCallsA, Description = "Cháu – Chú Bác Cô Dì", Distance = depthA + depthB };
                }

                bCallsA = GetDescendantTerm(depthA - depthB);
                aCallsB = $"Họ hàng (bậc {depthB})";
                return new KinshipResult { ACallsB = aCallsB, BCallsA = bCallsA, Description = $"Họ hàng cách {depthA + depthB} bậc", Distance = depthA + depthB };
            }
        }
    }
}
