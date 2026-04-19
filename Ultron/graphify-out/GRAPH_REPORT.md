# Graph Report - D:\Project Ultron\Ultron  (2026-04-19)

## Corpus Check
- 5 files · ~1,493 words
- Verdict: corpus is large enough that graph structure adds value.

## Summary
- 18 nodes · 15 edges · 6 communities detected
- Extraction: 87% EXTRACTED · 13% INFERRED · 0% AMBIGUOUS · INFERRED: 2 edges (avg confidence: 0.8)
- Token cost: 0 input · 0 output

## Community Hubs (Navigation)
- [[_COMMUNITY_Community 0|Community 0]]
- [[_COMMUNITY_Community 1|Community 1]]
- [[_COMMUNITY_Community 2|Community 2]]
- [[_COMMUNITY_Community 3|Community 3]]
- [[_COMMUNITY_Community 4|Community 4]]
- [[_COMMUNITY_Community 5|Community 5]]

## God Nodes (most connected - your core abstractions)
1. `UltronController` - 4 edges
2. `ClaudeService` - 2 edges
3. `NewsService` - 2 edges
4. `Ultron.API.Controllers` - 1 edges
5. `ChatRequest` - 1 edges
6. `Ultron.API.Services` - 1 edges
7. `Ultron.API.Services` - 1 edges

## Surprising Connections (you probably didn't know these)
- None detected - all connections are within the same source files.

## Communities

### Community 0 - "Community 0"
Cohesion: 0.4
Nodes (2): NewsService, Ultron.API.Services

### Community 1 - "Community 1"
Cohesion: 0.5
Nodes (2): ClaudeService, Ultron.API.Services

### Community 2 - "Community 2"
Cohesion: 0.67
Nodes (2): ChatRequest, Ultron.API.Controllers

### Community 3 - "Community 3"
Cohesion: 0.67
Nodes (2): ControllerBase, UltronController

### Community 4 - "Community 4"
Cohesion: 1.0
Nodes (0): 

### Community 5 - "Community 5"
Cohesion: 1.0
Nodes (0): 

## Knowledge Gaps
- **4 isolated node(s):** `Ultron.API.Controllers`, `ChatRequest`, `Ultron.API.Services`, `Ultron.API.Services`
  These have ≤1 connection - possible missing edges or undocumented components.
- **Thin community `Community 4`** (2 nodes): `main.js`, `createWindow()`
  Too small to be a meaningful cluster - may be noise or needs more connections extracted.
- **Thin community `Community 5`** (1 nodes): `Program.cs`
  Too small to be a meaningful cluster - may be noise or needs more connections extracted.

## Suggested Questions
_Questions this graph is uniquely positioned to answer:_

- **Why does `UltronController` connect `Community 3` to `Community 0`, `Community 2`?**
  _High betweenness centrality (0.382) - this node is a cross-community bridge._
- **What connects `Ultron.API.Controllers`, `ChatRequest`, `Ultron.API.Services` to the rest of the system?**
  _4 weakly-connected nodes found - possible documentation gaps or missing edges._