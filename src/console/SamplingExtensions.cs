// FlashGrader: minimal sketch of grading one flashcard answer with an LLM.
// Uses LlmClient to encapsulate HttpClient while keeping the wire protocol visible.
//
// Run: start LM Studio's server on port 1234 and load a chat LLM (not an embedding model).
//      dotnet run --project src/FlashGrader

public static class SamplingExtensions
{
    extension(Random rng)
    {
        public int[] SampleProportional(ReadOnlySpan<float> weights, int k)
        {
            int n = weights.Length;
            ArgumentOutOfRangeException.ThrowIfNegative(k);
            ArgumentOutOfRangeException.ThrowIfGreaterThan(k, n);

            double total = 0; // accumulate in double to limit rounding error
            foreach (float w in weights)
            {
                if (!(w >= 0) || float.IsInfinity(w))
                    throw new ArgumentOutOfRangeException(nameof(weights));
                total += w;
            }
            if (k == 0) return [];

            double step = total / k;
            foreach (float w in weights)
                if (w > step * (1 + 1e-5)) // tolerate float rounding from the caller's capping
                    throw new ArgumentException("Weight too large for exact proportional inclusion.");

            int[] order = new int[n];
            for (int i = 0; i < n; i++) order[i] = i;
            rng.Shuffle(order);                     // randomizes which items co-occur

            var result = new int[k];
            double u = rng.NextDouble() * step;     // first point
            double cum = 0;
            int j = 0;
            for (int t = 0; t < n && j < k; t++)
            {
                int i = order[t];
                // treat the last segment as open-ended to absorb rounding error
                cum = t == n - 1 ? double.PositiveInfinity : cum + weights[i];
                if (u < cum)                         // at most one point per segment
                {
                    result[j++] = i;
                    u += step;
                }
            }
            return result;
        }
    }
}