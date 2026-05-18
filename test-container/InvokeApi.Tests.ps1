param(
    [string]$sdkTag = '8.0.421',
    [string]$aspNetTag = '8.0'
)

Describe 'Docker API Integration' {
    BeforeAll {
        # Build the Docker image
        docker build --tag testapi --build-arg sdkTag=$sdkTag --build-arg aspNetTag=$aspNetTag .
    }

    BeforeEach {
        # Run the Docker container (if not already running)
        $null = docker ps -q -f name=testapi
        if (-not (docker ps -q -f name=testapi)) {
            docker run --rm -d -p 7175:8080 --name testapi testapi | Out-Null
            Start-Sleep -Seconds 5 # Wait for container to start
        }
    }

    It 'Responds to /weatherforecast' {
        $req = Invoke-WebRequest http://localhost:7175/weatherforecast
        $req.StatusCode | Should -Be 200
    }

    It 'Responds to /options' {
        $req = Invoke-WebRequest http://localhost:7175/options
        $req.StatusCode | Should -Be 200
    }

    AfterAll {
        # Stop the Docker container if running
        if (docker ps -q -f name=testapi) {
            # docker stop testapi | Out-Null
        }
    }
}
