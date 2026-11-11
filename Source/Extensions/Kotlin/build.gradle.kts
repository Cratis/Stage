import com.google.protobuf.gradle.id

plugins {
    kotlin("jvm") version "2.3.21"
    id("com.google.protobuf") version "0.10.1"
    `maven-publish`
}

group = "io.cratis.stage"
version = "1.0.0"

val grpcVersion = "1.80.0"
val grpcKotlinVersion = "1.5.0"
val protobufVersion = "4.34.1"

kotlin {
    jvmToolchain(17)
    explicitApi()
}

dependencies {
    api("io.grpc:grpc-stub:$grpcVersion")
    api("io.grpc:grpc-protobuf:$grpcVersion")
    api("io.grpc:grpc-kotlin-stub:$grpcKotlinVersion")
    api("com.google.protobuf:protobuf-kotlin:$protobufVersion")
    api("org.jetbrains.kotlinx:kotlinx-coroutines-core:1.11.0")
    implementation("io.grpc:grpc-netty-shaded:$grpcVersion")

    testImplementation(kotlin("test"))
    testImplementation("com.google.protobuf:protobuf-java-util:$protobufVersion")
    testImplementation("com.google.code.gson:gson:2.13.2")
    testImplementation("org.jetbrains.kotlinx:kotlinx-coroutines-test:1.11.0")
}

sourceSets {
    create("example") {
        kotlin.srcDir("examples")
        compileClasspath += sourceSets["main"].output + configurations["testRuntimeClasspath"]
        runtimeClasspath += output + compileClasspath
    }
    main {
        proto {
            // The extension protocol: the files every SDK builds from.
            srcDir("../Protocol")
        }
    }
}

protobuf {
    protoc { artifact = "com.google.protobuf:protoc:$protobufVersion" }
    plugins {
        id("grpc") { artifact = "io.grpc:protoc-gen-grpc-java:$grpcVersion" }
        id("grpckt") { artifact = "io.grpc:protoc-gen-grpc-kotlin:$grpcKotlinVersion:jdk8@jar" }
    }
    generateProtoTasks {
        all().forEach {
            it.plugins {
                id("grpc")
                id("grpckt")
            }
            it.builtins {
                id("kotlin")
            }
        }
    }
}

tasks.register<JavaExec>("runExample") {
    group = "application"
    description = "Runs the minimal example extension on port 50051."
    classpath = sourceSets["example"].runtimeClasspath
    mainClass.set("io.cratis.stage.extensions.examples.HelloKt")
}

tasks.test {
    useJUnitPlatform()
    // The conformance cases every SDK passes.
    systemProperty("conformance.cases", file("../Conformance/cases.json").absolutePath)
}

publishing {
    publications {
        create<MavenPublication>("sdk") {
            artifactId = "stage-extensions"
            from(components["java"])
        }
    }
}
