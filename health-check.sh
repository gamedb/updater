#!/usr/bin/env bash

lines=$(find last-changenumber.txt -mmin -5 -type f -print | wc -l)

if [[ ${lines} -eq 0 ]]; then

    exit 1

fi

exit 0
