import json, os

with open("graphify-out/graph.json", "r", encoding="utf-8") as f:
    data = json.load(f)

out_dir = "obsidian-notes"
os.makedirs(out_dir, exist_ok=True)

def clean(x):
    return x.replace("/", "_").replace("\\", "_")

# create all nodes first
for node in data.get("nodes", []):
    name = clean(node["id"])
    with open(f"{out_dir}/{name}.md", "w", encoding="utf-8") as f:
        f.write(f"# {name}\n\n")

# then add links
for edge in data.get("edges", []):
    src = clean(edge["source"])
    tgt = clean(edge["target"])

    with open(f"{out_dir}/{src}.md", "a", encoding="utf-8") as f:
        f.write(f"- [[{tgt}]]\n")