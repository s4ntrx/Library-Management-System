# Hosting the project on GitHub

**GitHub hosts this project's source code and its downloadable builds. It does not run the program for you.** A console application is not hosted the way a website is. People get it by cloning the repository or downloading a release, then run it on their own machine. If you later want a web version that runs in a browser, that is a different project (ASP.NET Core) and would need a web host.

The steps below take you from a folder on your computer to a public repository with automatic tests and downloadable releases.

## 1. Create a GitHub account

Sign up at github.com, then turn on two-factor authentication under Settings, Password and authentication.

## 2. Tell git who you are

```bash
git config --global user.name "Your Name"
git config --global user.email "you@example.com"
git config --global init.defaultBranch main
```

To keep your email private, use the no-reply address GitHub shows under Settings, Emails.

## 3. Set up an SSH key

This lets you push without typing a password each time.

```bash
ssh-keygen -t ed25519 -C "you@example.com"
cat ~/.ssh/id_ed25519.pub
```

Press Enter to accept the default file location, and choose a passphrase. Copy the line printed by `cat`. On GitHub, open Settings, SSH and GPG keys, New SSH key, paste it, and save. Then test:

```bash
ssh -T git@github.com
```

The first time, answer `yes` to the fingerprint question. A message saying you have successfully authenticated means it works.

## 4. Create the empty repository on GitHub

1. Click New repository.
2. Name it `library-management-system`.
3. Choose Public or Private.
4. Leave "Add a README", ".gitignore" and "license" **unticked**. The project already has them, and ticking them creates a conflict on your first push.
5. Click Create repository.

## 5. Push the project

From inside the project folder:

```bash
git init -b main
git add .
git status
```

Read the `git status` output. You should see source files, docs and workflows. You should **not** see `bin/`, `obj/` or any `library.json`. If you do, the `.gitignore` file is missing.

```bash
git commit -m "Initial commit: library management system"
git remote add origin git@github.com:YOUR-USERNAME/library-management-system.git
git push -u origin main
```

Replace `YOUR-USERNAME` with your GitHub username. Refresh the repository page and your files should be there, with the README displayed and the architecture diagram drawn.

## 6. Check the automatic tests

Open the Actions tab on GitHub. A workflow named CI should be running. A green tick means the project builds and every test passes on a clean machine. A red cross means something fails there; click it to read the log.

To show the status on your README, add this line under the title, with your username:

```markdown
![CI](https://github.com/YOUR-USERNAME/library-management-system/actions/workflows/ci.yml/badge.svg)
```

## 7. Publish a release

Tag a version and push the tag:

```bash
git tag v1.0.0
git push origin v1.0.0
```

The Release workflow builds single-file programs for Linux and Windows. After a few minutes, open the Releases page and you will find two zip files attached.

## 8. Run a release on Parrot OS

This needs no .NET installation, because the release is self-contained.

```bash
unzip library-v1.0.0-linux-x64.zip -d library-app
cd library-app
chmod +x library
./library
```

## 9. Finish the repository page

- Click the gear next to About, add a one-line description, and add topics such as `csharp`, `dotnet`, `console-app`, `library-management`.
- If the project is for a course, add the case study link to the README.
- The MIT license names "Library Management System contributors". Replace that line with your name in `LICENSE` if you want personal credit.

## 10. Everyday workflow

```bash
git switch -c fix-search-bug       # work on a branch
# edit files
dotnet test                        # check before committing
git add -A
git commit -m "Fix search to ignore leading spaces"
git push -u origin fix-search-bug  # then open a Pull Request on GitHub
```

CI runs on every pull request, so broken changes are caught before they reach `main`.

## Common problems

| Problem | Fix |
|---|---|
| `Permission denied (publickey)` | The SSH key was not added to GitHub, or `ssh -T git@github.com` was not tested. Redo step 3. |
| `failed to push some refs` on first push | You ticked README or license when creating the repository. Run `git pull origin main --allow-unrelated-histories`, resolve any conflicts, then push. |
| `bin/` and `obj/` appear in `git status` | `.gitignore` is missing. If already committed, run `git rm -r --cached src tests` then `git add .` and commit again. |
| Release workflow did not run | The tag must start with `v` and must be pushed with `git push origin v1.0.0`. Creating it locally is not enough. |
| Release workflow fails with a permissions error | In the repository, open Settings, Actions, General, Workflow permissions and choose Read and write. |
