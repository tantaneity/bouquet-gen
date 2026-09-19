using System;
using System.Collections.Generic;
using UnityEngine;

internal static class StalkClearance
{
    private const int ClearancePasses = 4;
    private const float HeadClearance = 1.05f;

    // every stalk is a capsule running on from its tip (a bloom's is just a ball).
    // where one overlaps a bloom head its target is walked out of the ball, so leaves
    // lie beside the petals instead of through them. of two heads the smaller gives way
    public static void Resolve(List<Stalk> plan, Func<Stalk, Vector3, Stalk> regrow)
    {
        for (int pass = 0; pass < ClearancePasses; pass++)
        {
            for (int i = 0; i < plan.Count; i++)
            {
                Stalk stalk = plan[i];
                Vector3 push = WithoutSinking(OverlapWithHeads(plan, i, stalk), stalk.target);
                if (push.sqrMagnitude > 1e-8f)
                {
                    plan[i] = regrow(stalk, stalk.target + push);
                }
            }
        }
    }

    private static float BodyRadius(Stalk stalk)
    {
        return stalk.species.IsBloom()
            ? stalk.headSize * HeadClearance
            : stalk.species.SprayRadius() * stalk.headSize;
    }

    private static Vector3 OverlapWithHeads(List<Stalk> plan, int self, Stalk stalk)
    {
        Vector3 start = stalk.tip;
        Vector3 end = stalk.tip + stalk.axis * stalk.species.TipReserve() * stalk.headSize;
        float radius = BodyRadius(stalk);
        bool isBloom = stalk.species.IsBloom();
        Vector3 push = Vector3.zero;

        for (int j = 0; j < plan.Count; j++)
        {
            Stalk head = plan[j];
            if (j == self || !head.species.IsBloom())
            {
                continue;
            }

            if (isBloom && (head.headSize < stalk.headSize || (head.headSize == stalk.headSize && j > self)))
            {
                continue;
            }

            Vector3 nearest = NearestOnSegment(start, end, head.tip);
            Vector3 away = nearest - head.tip;
            float distance = away.magnitude;
            float overlap = BodyRadius(head) + radius - distance;
            if (overlap <= 0.0f)
            {
                continue;
            }

            Vector3 direction = distance > 1e-4f ? away / distance : Vector3.Cross(stalk.axis, Vector3.up).normalized;
            push += direction * overlap;
        }

        return push;
    }

    // sliding a stalk back toward the tie would bury its head in the bundle, and
    // pushing it down lays it flat across the ribbon, so only the sideways, outward
    // and upward part of a push is kept
    private static Vector3 WithoutSinking(Vector3 push, Vector3 target)
    {
        push.y = Mathf.Max(push.y, 0.0f);
        Vector3 reach = target.normalized;
        return push - reach * Mathf.Min(0.0f, Vector3.Dot(push, reach));
    }

    private static Vector3 NearestOnSegment(Vector3 start, Vector3 end, Vector3 point)
    {
        Vector3 span = end - start;
        float t = Mathf.Clamp01(Vector3.Dot(point - start, span) / Mathf.Max(span.sqrMagnitude, 1e-8f));
        return start + span * t;
    }
}
