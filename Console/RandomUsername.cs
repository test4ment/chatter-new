namespace chatter_new_console;

public static class RandomUsername {
    private static readonly Random random = new();

    private static readonly string[] adj = [
        "swift", "golden", "silent", "brave", "cosmic",
        "frosty", "lucky", "wild", "mystic", "clever",
        "iron", "crimson", "jolly", "neon", "shadow",
        "velvet", "savage", "wicked", "proud", "ancient",
        "silver", "rusty", "stellar", "quiet", "pretty",
        "ambivalent", "platinum", "stone", "modern", "anonymous",
        "witty", "smelly", "majestic", "gray", "rouge",
    ];
    private static readonly string[] nouns = [
        "fox", "wolf", "hawk", "storm", "viper",                    
        "phantom", "titan", "raven", "cobra", "drift",              
        "spark", "ember", "blade", "frost", "bolt",                 
        "summit", "pixel", "cipher", "oracle", "atlas",
        "apple", "computer", "world", "disco", "potato",
        "tank", "ship", "mind", "block", "byte", "leap",
        "cat", "tree", "shell", "noodle", "vector", "biscuit",
    ];
    
    public static string Generate() {
        return adj[random.Next(adj.Length)] + "-" + nouns[random.Next(nouns.Length)];
    }
}