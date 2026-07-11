#!/usr/bin/env python3
"""Summarise a Kinect Reframe .krs.json recording using only the Python standard library."""

from __future__ import annotations

import argparse
import json
import math
from collections import defaultdict
from pathlib import Path
from statistics import mean
from typing import Any


def distance(sample: dict[str, Any]) -> float:
    """Return the 3D distance between the raw and smoothed joint positions."""
    return math.sqrt(
        (float(sample["RawX"]) - float(sample["SmoothedX"])) ** 2
        + (float(sample["RawY"]) - float(sample["SmoothedY"])) ** 2
        + (float(sample["RawZ"]) - float(sample["SmoothedZ"])) ** 2
    )


def load_recording(path: Path) -> dict[str, Any]:
    try:
        with path.open("r", encoding="utf-8") as handle:
            payload = json.load(handle)
    except (OSError, json.JSONDecodeError) as exc:
        raise SystemExit(f"Could not read {path}: {exc}") from exc

    if payload.get("Format") != "kinect-reframe-session-v1":
        raise SystemExit(
            f"Unsupported recording format: {payload.get('Format', '<missing>')}"
        )

    return payload


def analyse(payload: dict[str, Any]) -> None:
    frames = payload.get("Frames", [])
    if not frames:
        print("The recording contains no frames.")
        return

    duration_ms = int(frames[-1].get("TimestampMilliseconds", 0))
    duration_seconds = duration_ms / 1000.0
    fps = len(frames) / duration_seconds if duration_seconds > 0 else 0.0

    displacement_by_joint: dict[str, list[float]] = defaultdict(list)
    predicted_by_joint: dict[str, int] = defaultdict(int)
    inferred_by_joint: dict[str, int] = defaultdict(int)
    samples_by_joint: dict[str, int] = defaultdict(int)

    for frame in frames:
        for sample in frame.get("Joints", []):
            joint = str(sample.get("Joint", "Unknown"))
            samples_by_joint[joint] += 1
            displacement_by_joint[joint].append(distance(sample))

            if bool(sample.get("Predicted", False)):
                predicted_by_joint[joint] += 1
            if sample.get("TrackingState") == "Inferred":
                inferred_by_joint[joint] += 1

    print(f"Created:       {payload.get('CreatedUtc', '<unknown>')}")
    print(f"Seated mode:   {payload.get('SeatedMode', False)}")
    print(f"Frames:        {len(frames)}")
    print(f"Duration:      {duration_seconds:.2f} seconds")
    print(f"Average FPS:   {fps:.2f}")
    print()
    print("Joint                    avg correction   inferred   predicted")
    print("-----------------------  ---------------  ---------  ---------")

    for joint in sorted(samples_by_joint):
        corrections = displacement_by_joint[joint]
        correction_mm = mean(corrections) * 1000.0 if corrections else 0.0
        print(
            f"{joint:<23}  {correction_mm:>10.2f} mm"
            f"  {inferred_by_joint[joint]:>9}"
            f"  {predicted_by_joint[joint]:>9}"
        )


def parse_args() -> argparse.Namespace:
    parser = argparse.ArgumentParser(
        description="Summarise a Kinect Reframe .krs.json recording."
    )
    parser.add_argument("recording", type=Path, help="Path to a .krs.json file")
    return parser.parse_args()


def main() -> None:
    args = parse_args()
    analyse(load_recording(args.recording))


if __name__ == "__main__":
    main()
