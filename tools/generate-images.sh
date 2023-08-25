#!/bin/bash

# Deps
xbps-install -Syu > /dev/null
xbps-install -y ImageMagick yarn > /dev/null
yarn global add svgo > /dev/null

# Functions
regenerate()
{
  echo "Generating assets for $1"

  # Optimize the SVG file
  svgo --multipass --quiet "$1"

  # Convert to PNG
  convert -background none "$1" "${1%.*}.png"

  # Convert to ICO
  convert -background transparent -define "icon:auto-resize=16,24,32,64,128,256" "$1" "${1%.*}.ico"
}

# Iterate over each file matching the pattern "*.svg" in the "img" directory
for file in img/*.svg; do
    # Execute the "regenerate" command on each file
    regenerate "$file"
done

# Check if any files were modified
git config --global user.email "github-actions[bot]@users.noreply.github.com"
git config --global user.name "github-actions[bot]"
git add img > /dev/null
git diff-index --quiet HEAD
if [ "$?" == "1" ]; then
  git commit -m "[ci-skip] Regenerate image files." > /dev/null
  git push > /dev/null
else
  echo "No image files were modified."
fi