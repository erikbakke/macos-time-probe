macOS time probe kit
====================

What it is: MacTimeProbe.cs (a single-file .NET program) plus a workflow that runs it on GitHub's hosted macOS runners. It prints, for each of the 21 rows of the
OceTimeTests parity table, whether the macOS version's own data gives the right clock time (zone), date text and time text. It holds no customer data and no secrets.
It does contain a few lines of pattern logic copied from OceTime (the spacing reduction and the date and time patterns); whether that is fine to put in a public
repository is the owner's decision.

How to run it (about ten minutes, no card):
  1. On github.com, signed in with a PERSONAL account, create a new PUBLIC repository (any name, no README). Public repositories get the hosted macOS runners free.
  2. Put the contents of this folder in it, keeping the paths:  .github/workflows/probe.yml  and  MacTimeProbe.cs  (at the root).
  3. Push to the main branch. The workflow starts on its own (or use Actions > macOS time probe > Run workflow).
  4. When it finishes, open each job (macos-latest, macos-15, macos-14), expand "Probe", and copy the whole log, or download the probe-output artifact.
  5. Send the output back. Each job begins with the exact macOS version, so the results can be compared with each other and with Pieter's Mac.

Notes:
  - If a job fails straight away with "no runner" or similar, its macOS label no longer exists: edit the matrix in probe.yml to the current labels.
  - Delete the repository when you are done.
