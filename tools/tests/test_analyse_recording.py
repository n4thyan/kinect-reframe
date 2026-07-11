import math
import unittest

from tools.analyse_recording import distance


class DistanceTests(unittest.TestCase):
    def test_distance_uses_all_three_axes(self):
        sample = {
            "RawX": 0.0,
            "RawY": 0.0,
            "RawZ": 0.0,
            "SmoothedX": 0.03,
            "SmoothedY": 0.04,
            "SmoothedZ": 0.12,
        }

        self.assertTrue(math.isclose(distance(sample), 0.13, rel_tol=1e-9))

    def test_distance_is_zero_for_identical_points(self):
        sample = {
            "RawX": 1.0,
            "RawY": -2.0,
            "RawZ": 3.5,
            "SmoothedX": 1.0,
            "SmoothedY": -2.0,
            "SmoothedZ": 3.5,
        }

        self.assertEqual(distance(sample), 0.0)


if __name__ == "__main__":
    unittest.main()
