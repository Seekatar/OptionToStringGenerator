## PR Checklist

- [ ] CHANGELOG.md has been updated with a new version number and a description of the changes
- [ ] All tests related to the changed code pass
- [ ] No new warnings or errors in the build
- [ ] No vulnerable packages
- [ ] Running against all supported .NET SDK versions in the test container works with `./run.ps1 pack,testBuilds -Vers 0.3.x.y`
