
window.math3D_instances = {};

window.math3D = {
    loadModel: function (canvasId, modelPath) {
        const existing = window.math3D_instances[canvasId];
        if (existing) {
            return;
        }

        const canvas = document.getElementById(canvasId);
        if (!canvas) return;

        if (window.math3D_instances[canvasId]) return;

        const scene = new THREE.Scene();

        const width = canvas.parentElement.clientWidth;
        const height = canvas.parentElement.clientHeight;

        const camera = new THREE.PerspectiveCamera(45, width / height, 0.1, 100);
        camera.position.z = 1.5;

        const renderer = new THREE.WebGLRenderer({ canvas: canvas, alpha: true, antialias: true });
        renderer.setSize(width, height);
        renderer.setPixelRatio(window.devicePixelRatio);

        const pmremGenerator = new THREE.PMREMGenerator(renderer);
        const environment = new THREE.RoomEnvironment();
        scene.environment = pmremGenerator.fromScene(environment, 0.01).texture;

        const hemiLight = new THREE.HemisphereLight(0xffffff, 0x444444, 5);
        scene.add(hemiLight);

        const loader = new THREE.GLTFLoader();
        let loadedModel = null;

        loader.load(modelPath, function (gltf) {
            loadedModel = gltf.scene;

            loadedModel.traverse((node) => {
                if (node.isMesh) {
                    node.material.emissiveIntensity = 2.5;
                }
            });

            const box = new THREE.Box3().setFromObject(loadedModel);
            const size = box.getSize(new THREE.Vector3());
            const maxDim = Math.max(size.x, size.y, size.z);
            const scale = 2 / maxDim;
            loadedModel.scale.setScalar(scale);

            const center = box.getCenter(new THREE.Vector3());
            loadedModel.position.x -= center.x;
            loadedModel.position.y -= center.y;
            loadedModel.position.z -= center.z;

            loadedModel.rotation.x = Math.PI / 2;

            scene.add(loadedModel);
        }, undefined, function (error) {
            console.error('Error at loading the .glb file:', error);
        });

        let animationId;
        const controls = new THREE.OrbitControls(camera, canvas);
        controls.enableZoom = true;
        controls.enablePan = false;

        const animate = function () {
            animationId = requestAnimationFrame(animate);
            renderer.render(scene, camera);
            controls.update();
        };
        animate(scene, camera);

        window.math3D_instances[canvasId] = {
            animationId: animationId,
            renderer: renderer,
            scene: scene
        };
    },

    stopModel: function (canvasId) {
        const instance = window.math3D_instances[canvasId];
        if (instance) {
            cancelAnimationFrame(instance.animationId);
            instance.renderer.dispose();
            delete window.math3D_instances[canvasId];
        }
    }
};

