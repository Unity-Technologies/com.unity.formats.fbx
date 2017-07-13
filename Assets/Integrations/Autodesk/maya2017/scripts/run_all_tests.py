#!/usr/bin/python

import unittest

if __name__ == '__main__':
    testsuite = unittest.TestLoader().discover('.', pattern='*.py')
    unittest.TextTestRunner(verbosity=1).run(testsuite)