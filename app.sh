#!/bin/bash


if [ "$TRAVIS_BRANCH" = "uat" ]
then
    CF_SPACE="uat"
fi

if [ "$TRAVIS_BRANCH" = "training" ]
then
    CF_SPACE="training"
fi

if [ "$TRAVIS_BRANCH" = "testing" ]
then
    CF_SPACE="test"
fi

if [ "$TRAVIS_BRANCH" = "pre-production" ]
then
    CF_SPACE="preprod"
fi

if [ "$TRAVIS_BRANCH" = "sandbox" ]
then
    CF_SPACE="sand"
fi

if [ "$TRAVIS_BRANCH" = "production" ]
then
    CF_SPACE="prod"
fi

echo "$CF_SPACE"

sed "s/CF_SPACE/$CF_SPACE/g" manifest-template.yml > manifest.yml