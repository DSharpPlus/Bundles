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
  convert -size 1024x1024 -background transparent "$1" "${1%.*}.png"

  # Convert to ICO
  # https://stackoverflow.com/a/15104985
  convert -background transparent -colors 256 "$1" \
    \( -clone 0 -resize 16x16 \) \
    \( -clone 0 -resize 32x32 \) \
    \( -clone 0 -resize 48x48 \) \
    \( -clone 0 -resize 64x64 \) \
    \( -clone 0 -resize 128x128 \) \
    \( -clone 0 -resize 256x256 \) \
    -delete 0 "${1%.*}.ico"
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