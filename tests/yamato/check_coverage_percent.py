### Implementation taken from https://github.com/Unity-Technologies/ml-agents/pull/3642/files

from __future__ import print_function
import sys
import os

SUMMARY_XML_FILENAME = "Summary.xml"

# Note that this is python2 compatible, since that's currently what's installed on most CI images.

def check_coverage(root_dir, min_percentage):
    # Walk the root directory looking for the summary file that
    # is output by the code coverage checks. It's possible that
    # we'll need to refine this later in case there are multiple
    # such files.
    summary_xml = None
    for dirpath, _, filenames in os.walk(root_dir):
        if SUMMARY_XML_FILENAME in filenames:
            summary_xml = os.path.join(dirpath, SUMMARY_XML_FILENAME)
            break
    if not summary_xml:
        print("Couldn't find {} in root directory".format(SUMMARY_XML_FILENAME))
        sys.exit(1)

    with open(summary_xml) as f:
        # Rather than try to parse the XML, just look for a line of the form
        # <Linecoverage>73.9</Linecoverage>
        lines = f.readlines()
        for l in lines:
            if "Linecoverage" in l:
                pct = l.replace("<Linecoverage>", "").replace("</Linecoverage>", "")
                pct = float(pct)
                if pct < min_percentage:
                    print(
                        "Coverage {} is below the min percentage of {}.".format(
                            pct, min_percentage
                        )
                    )
                    sys.exit(1)
                else:
                    print(
                        "Coverage {} is above the min percentage of {}.".format(
                            pct, min_percentage
                        )
                    )
                    sys.exit(0)

    # Couldn't find the results in the file.
    print("Couldn't find Linecoverage in summary file")
    sys.exit(1)


def main():
    root_dir = sys.argv[1]
    min_percent = float(sys.argv[2])
    if min_percent > 0:
        check_coverage(root_dir, min_percent)


if __name__ == "__main__":
    main()